using GLMS.Tests.TestSupport;
using GLMS.Api.Data;
using GLMS.Api.Models;
using GLMS.Api.Services.Contracts;
using GLMS.Shared.DTOs;
using GLMS.Shared.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace GLMS.Tests;

public class ContractServiceTests
{
    [Fact]
    public async Task ChangeStatusAsync_ApproveDraft_MovesToActive()
    {
        var (service, options, tempRoot) = CreateSut();
        try
        {
            await SeedContractAsync(options, 300, ContractStatus.Draft);

            var result = await service.ChangeStatusAsync(300, ContractTransitionAction.Approve);

            Assert.True(result.Succeeded);
            Assert.Equal(ContractStatus.Draft, result.PreviousStatus);
            Assert.Equal(ContractStatus.Active, result.CurrentStatus);

            await using var db = new AppDbContext(options);
            var stored = await db.Contracts.SingleAsync(c => c.ContractId == 300);
            Assert.Equal(ContractStatus.Active, stored.Status);
        }
        finally
        {
            Cleanup(tempRoot);
        }
    }

    [Fact]
    public async Task ChangeStatusAsync_SuspendDraft_ReturnsFailure()
    {
        var (service, options, tempRoot) = CreateSut();
        try
        {
            await SeedContractAsync(options, 301, ContractStatus.Draft);

            var result = await service.ChangeStatusAsync(301, ContractTransitionAction.Suspend);

            Assert.False(result.Succeeded);
            Assert.Contains("cannot be suspended", result.Message, StringComparison.OrdinalIgnoreCase);

            await using var db = new AppDbContext(options);
            var stored = await db.Contracts.SingleAsync(c => c.ContractId == 301);
            Assert.Equal(ContractStatus.Draft, stored.Status);
        }
        finally
        {
            Cleanup(tempRoot);
        }
    }

    [Fact]
    public async Task UploadAgreementAsync_RejectsNonPdfFile()
    {
        var (service, options, tempRoot) = CreateSut();
        try
        {
            await SeedContractAsync(options, 302, ContractStatus.Active);

            var badTextPath = GetTestDataFilePath("bad.txt");
            await using var stream = File.OpenRead(badTextPath);
            var result = await service.UploadAgreementAsync(302, "bad.txt", "text/plain", stream.Length, stream);

            Assert.False(result.Succeeded);
            Assert.Contains("Only .pdf files are allowed", result.Message);

            await using var db = new AppDbContext(options);
            var contract = await db.Contracts.SingleAsync(c => c.ContractId == 302);
            Assert.Null(contract.PdfFileName);
            Assert.Null(contract.PdfOriginalFileName);
        }
        finally
        {
            Cleanup(tempRoot);
        }
    }

    [Fact]
    public async Task UploadAgreementAsync_RejectsPdfExtensionWithNonPdfContentType()
    {
        var (service, options, tempRoot) = CreateSut();
        try
        {
            await SeedContractAsync(options, 304, ContractStatus.Active);

            var badPdfPath = GetTestDataFilePath("bad.pdf");
            await using var stream = File.OpenRead(badPdfPath);
            var result = await service.UploadAgreementAsync(304, "bad.pdf", "text/plain", stream.Length, stream);

            Assert.False(result.Succeeded);
            Assert.Contains("must have a PDF content type", result.Message, StringComparison.OrdinalIgnoreCase);

            await using var db = new AppDbContext(options);
            var contract = await db.Contracts.SingleAsync(c => c.ContractId == 304);
            Assert.Null(contract.PdfFileName);
            Assert.Null(contract.PdfOriginalFileName);
        }
        finally
        {
            Cleanup(tempRoot);
        }
    }

    [Fact]
    public async Task UploadAgreementAsync_RejectsPdfExtensionWithInvalidPdfHeader()
    {
        var (service, options, tempRoot) = CreateSut();
        try
        {
            await SeedContractAsync(options, 305, ContractStatus.Active);

            var badPdfPath = GetTestDataFilePath("bad.pdf");
            await using var stream = File.OpenRead(badPdfPath);
            var result = await service.UploadAgreementAsync(305, "bad.pdf", "application/pdf", stream.Length, stream);

            Assert.False(result.Succeeded);
            Assert.Contains("not a valid PDF", result.Message, StringComparison.OrdinalIgnoreCase);

            await using var db = new AppDbContext(options);
            var contract = await db.Contracts.SingleAsync(c => c.ContractId == 305);
            Assert.Null(contract.PdfFileName);
            Assert.Null(contract.PdfOriginalFileName);
        }
        finally
        {
            Cleanup(tempRoot);
        }
    }

    [Fact]
    public async Task UploadAgreementAsync_AcceptsPdf_StoresUuidNamedFile_AndMetadata()
    {
        var (service, options, tempRoot) = CreateSut();
        try
        {
            await SeedContractAsync(options, 303, ContractStatus.Active);

            var validPdfPath = GetTestDataFilePath("test.pdf");
            await using var stream = File.OpenRead(validPdfPath);
            var result = await service.UploadAgreementAsync(303, "test.pdf", "application/pdf", stream.Length, stream);

            Assert.True(result.Succeeded);
            Assert.NotNull(result.StoredFileName);
            Assert.Matches("^[a-f0-9]{32}\\.pdf$", result.StoredFileName!);
            Assert.Equal("test.pdf", result.OriginalFileName);

            var storedPath = Path.Combine(tempRoot, "uploads", "contracts", result.StoredFileName!);
            Assert.True(File.Exists(storedPath));

            await using var db = new AppDbContext(options);
            var contract = await db.Contracts.SingleAsync(c => c.ContractId == 303);
            Assert.Equal(result.StoredFileName, contract.PdfFileName);
            Assert.Equal("test.pdf", contract.PdfOriginalFileName);
        }
        finally
        {
            Cleanup(tempRoot);
        }
    }

    private static (ContractService Service, DbContextOptions<AppDbContext> Options, string TempRoot) CreateSut()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        var tempRoot = Path.Combine(Path.GetTempPath(), "glms-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        var hostEnvironment = new Mock<IWebHostEnvironment>();
        hostEnvironment.SetupGet(env => env.WebRootPath).Returns(tempRoot);

        var service = new ContractService(new TestDbContextFactory(options), hostEnvironment.Object);
        return (service, options, tempRoot);
    }

    private static async Task SeedContractAsync(DbContextOptions<AppDbContext> options, int contractId, ContractStatus status)
    {
        await using var db = new AppDbContext(options);
        db.Clients.Add(new Client
        {
            ClientId = contractId,
            Name = $"Client {contractId}",
            Email = $"client{contractId}@test.local",
            Phone = "+27 10 000 0000",
            Region = "Gauteng"
        });

        db.Contracts.Add(new Contract
        {
            ContractId = contractId,
            ClientId = contractId,
            Title = $"Contract {contractId}",
            ServiceLevel = "Gold",
            StartDate = DateTime.UtcNow.AddDays(-10),
            EndDate = DateTime.UtcNow.AddDays(60),
            Status = status
        });

        await db.SaveChangesAsync();
    }

    private static void Cleanup(string tempRoot)
    {
        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    private static string GetTestDataFilePath(string fileName)
    {
        var testDataPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "TestData", fileName));
        if (!File.Exists(testDataPath))
        {
            throw new FileNotFoundException($"Expected test data file not found: {testDataPath}", testDataPath);
        }

        return testDataPath;
    }
}
