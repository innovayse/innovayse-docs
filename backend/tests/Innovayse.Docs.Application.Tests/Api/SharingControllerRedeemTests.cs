using Innovayse.Docs.API.Sharing;
using Innovayse.Docs.Application.Documents;
using Innovayse.Docs.Application.Identity;
using Innovayse.Docs.Application.Notifications;
using Innovayse.Docs.Application.Sharing;
using Innovayse.Docs.Domain.Documents;
using Innovayse.Docs.Domain.Sharing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Innovayse.Docs.Application.Tests.Api;

public class SharingControllerRedeemTests
{
    private static SharingController BuildController(
        Mock<IShareLinkRepository> shareLinkRepository,
        Mock<IPermissionService> permissionService,
        Mock<IPermissionRepository> permissionRepository,
        Mock<IDocumentRepository> documentRepository,
        Guid callerId)
    {
        var controller = new SharingController(
            permissionRepository.Object,
            shareLinkRepository.Object,
            permissionService.Object,
            new Mock<ISsoUserLookupService>().Object,
            documentRepository.Object,
            new Mock<INotificationRepository>().Object,
            NullLogger<SharingController>.Instance);
        controller.SetCallerIdForTesting(callerId);
        return controller;
    }

    [Fact]
    public async Task RedeemLink_UnknownToken_ReturnsNotFound()
    {
        var documentId = Guid.NewGuid();
        var shareLinkRepository = new Mock<IShareLinkRepository>();
        shareLinkRepository.Setup(r => r.GetByTokenAsync("bad-token")).ReturnsAsync((ShareLink?)null);
        var controller = BuildController(shareLinkRepository, new Mock<IPermissionService>(),
            new Mock<IPermissionRepository>(), new Mock<IDocumentRepository>(), Guid.NewGuid());

        var result = await controller.RedeemLink(documentId, new SharingController.RedeemLinkRequest { Token = "bad-token" });

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task RedeemLink_TokenForDifferentDocument_ReturnsNotFound()
    {
        var documentId = Guid.NewGuid();
        var link = new ShareLink { Id = Guid.NewGuid(), DocumentId = Guid.NewGuid(), Token = "tok", Role = DocumentRole.Viewer };
        var shareLinkRepository = new Mock<IShareLinkRepository>();
        shareLinkRepository.Setup(r => r.GetByTokenAsync("tok")).ReturnsAsync(link);
        var controller = BuildController(shareLinkRepository, new Mock<IPermissionService>(),
            new Mock<IPermissionRepository>(), new Mock<IDocumentRepository>(), Guid.NewGuid());

        var result = await controller.RedeemLink(documentId, new SharingController.RedeemLinkRequest { Token = "tok" });

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task RedeemLink_ExpiredToken_ReturnsGone()
    {
        var documentId = Guid.NewGuid();
        var link = new ShareLink
        {
            Id = Guid.NewGuid(), DocumentId = documentId, Token = "tok", Role = DocumentRole.Viewer,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1),
        };
        var shareLinkRepository = new Mock<IShareLinkRepository>();
        shareLinkRepository.Setup(r => r.GetByTokenAsync("tok")).ReturnsAsync(link);
        var controller = BuildController(shareLinkRepository, new Mock<IPermissionService>(),
            new Mock<IPermissionRepository>(), new Mock<IDocumentRepository>(), Guid.NewGuid());

        var result = await controller.RedeemLink(documentId, new SharingController.RedeemLinkRequest { Token = "tok" });

        var statusResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status410Gone, statusResult.StatusCode);
    }

    [Fact]
    public async Task RedeemLink_ValidTokenCallerAlreadyHasAccess_NoOpsWithoutGranting()
    {
        var documentId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var link = new ShareLink { Id = Guid.NewGuid(), DocumentId = documentId, Token = "tok", Role = DocumentRole.Viewer };
        var shareLinkRepository = new Mock<IShareLinkRepository>();
        shareLinkRepository.Setup(r => r.GetByTokenAsync("tok")).ReturnsAsync(link);
        var permissionService = new Mock<IPermissionService>();
        permissionService.Setup(s => s.GetEffectiveRoleAsync(documentId, callerId)).ReturnsAsync(DocumentRole.Editor);
        var permissionRepository = new Mock<IPermissionRepository>();
        var controller = BuildController(shareLinkRepository, permissionService, permissionRepository,
            new Mock<IDocumentRepository>(), callerId);

        var result = await controller.RedeemLink(documentId, new SharingController.RedeemLinkRequest { Token = "tok" });

        Assert.IsType<NoContentResult>(result);
        permissionRepository.Verify(r => r.GrantAsync(It.IsAny<DocumentPermission>()), Times.Never);
    }

    [Fact]
    public async Task RedeemLink_ValidTokenNewCaller_GrantsLinkRole()
    {
        var documentId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var link = new ShareLink { Id = Guid.NewGuid(), DocumentId = documentId, Token = "tok", Role = DocumentRole.Commenter };
        var shareLinkRepository = new Mock<IShareLinkRepository>();
        shareLinkRepository.Setup(r => r.GetByTokenAsync("tok")).ReturnsAsync(link);
        var permissionService = new Mock<IPermissionService>();
        permissionService.Setup(s => s.GetEffectiveRoleAsync(documentId, callerId)).ReturnsAsync((DocumentRole?)null);
        var documentRepository = new Mock<IDocumentRepository>();
        documentRepository.Setup(r => r.GetByIdAsync(documentId))
            .ReturnsAsync(new Document { Id = documentId, OwnerId = ownerId });
        var permissionRepository = new Mock<IPermissionRepository>();
        DocumentPermission? granted = null;
        permissionRepository.Setup(r => r.GrantAsync(It.IsAny<DocumentPermission>()))
            .Callback<DocumentPermission>(p => granted = p)
            .Returns(Task.CompletedTask);
        var controller = BuildController(shareLinkRepository, permissionService, permissionRepository,
            documentRepository, callerId);

        var result = await controller.RedeemLink(documentId, new SharingController.RedeemLinkRequest { Token = "tok" });

        Assert.IsType<NoContentResult>(result);
        Assert.NotNull(granted);
        Assert.Equal(callerId, granted!.UserId);
        Assert.Equal(documentId, granted.DocumentId);
        Assert.Equal(DocumentRole.Commenter, granted.Role);
        Assert.Equal(ownerId, granted.GrantedBy);
    }
}
