using Innovayse.Docs.API.Documents;
using Innovayse.Docs.API.Versions;
using Innovayse.Docs.Application.Sharing;
using Innovayse.Docs.Domain.Sharing;
using Innovayse.Docs.Infrastructure.Persistence;
using Innovayse.Docs.Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Innovayse.Docs.Application.Tests.Api;

public class DocumentTabsIntegrationTests
{
    [Fact]
    public async Task TwoTabsOnSameDocument_HaveIndependentUpdateStreams()
    {
        var options = new DbContextOptionsBuilder<DocsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new DocsDbContext(options);
        var tabRepo = new DocumentTabRepository(context);
        var versionRepo = new VersionRepository(context);

        var callerId = Guid.NewGuid();
        var documentId = Guid.NewGuid();
        var permService = new Mock<IPermissionService>();
        permService.Setup(p => p.AuthorizeAsync(documentId, callerId, DocumentRole.Editor)).ReturnsAsync(true);
        permService.Setup(p => p.AuthorizeAsync(documentId, callerId, DocumentRole.Viewer)).ReturnsAsync(true);

        var tabsController = new DocumentTabsController(tabRepo, permService.Object);
        tabsController.SetCallerIdForTesting(callerId);
        var updatesController = new UpdatesController(versionRepo, permService.Object);
        updatesController.SetCallerIdForTesting(callerId);

        var tab1Result = await tabsController.Create(documentId, new DocumentTabsController.CreateTabRequest { Title = "Tab 1" });
        var tab1 = ((CreatedResult)tab1Result.Result!).Value as DocumentTabsController.TabResponse;
        var tab2Result = await tabsController.Create(documentId, new DocumentTabsController.CreateTabRequest { Title = "Tab 2" });
        var tab2 = ((CreatedResult)tab2Result.Result!).Value as DocumentTabsController.TabResponse;

        await updatesController.Append(documentId, tab1!.Id, new UpdatesController.AppendUpdateRequest { UpdateBase64 = Convert.ToBase64String(new byte[] { 1 }) });
        await updatesController.Append(documentId, tab2!.Id, new UpdatesController.AppendUpdateRequest { UpdateBase64 = Convert.ToBase64String(new byte[] { 2 }) });
        await updatesController.Append(documentId, tab2.Id, new UpdatesController.AppendUpdateRequest { UpdateBase64 = Convert.ToBase64String(new byte[] { 3 }) });

        var tab1Updates = (await updatesController.List(documentId, tab1.Id)).Result as OkObjectResult;
        var tab2Updates = (await updatesController.List(documentId, tab2.Id)).Result as OkObjectResult;

        var tab1List = Assert.IsType<List<UpdatesController.UpdateResponse>>(tab1Updates!.Value);
        var tab2List = Assert.IsType<List<UpdatesController.UpdateResponse>>(tab2Updates!.Value);
        Assert.Single(tab1List);
        Assert.Equal(2, tab2List.Count);
    }
}
