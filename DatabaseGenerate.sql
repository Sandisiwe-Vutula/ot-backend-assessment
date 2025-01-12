----------------------------------------------- Creating database
CREATE DATABASE OT_Assessment_DB;
GO

USE OT_Assessment_DB;
GO

----------------------------------------------- Creating tables
CREATE TABLE Player (
    AccountId UNIQUEIDENTIFIER PRIMARY KEY,
    Username NVARCHAR(100) NOT NULL
);


CREATE TABLE [Provider] (
    ProviderId INT IDENTITY PRIMARY KEY,
    [Name] NVARCHAR(200) NOT NULL UNIQUE
);

CREATE TABLE Game (
    GameId INT IDENTITY PRIMARY KEY,
    [Name] NVARCHAR(200) NOT NULL,
    Theme NVARCHAR(100) NOT NULL,
    ProviderId INT NOT NULL,
    FOREIGN KEY (ProviderId) REFERENCES [Provider](ProviderId)
);

CREATE TABLE CasinoWager (
    WagerId UNIQUEIDENTIFIER PRIMARY KEY,
    Theme NVARCHAR(100),
    [Provider] NVARCHAR(200),
    GameName NVARCHAR(200),
    TransactionId UNIQUEIDENTIFIER NOT NULL,
    BrandId UNIQUEIDENTIFIER,
    AccountId UNIQUEIDENTIFIER NOT NULL,
    Username NVARCHAR(100),
    ExternalReferenceId UNIQUEIDENTIFIER NOT NULL,
    TransactionTypeId UNIQUEIDENTIFIER NOT NULL,
    Amount DECIMAL(18, 2) NOT NULL,
    CreatedDateTime DATETIME2(3) NOT NULL,
    NumberOfBets INT NOT NULL,
    CountryCode NVARCHAR(10),
    SessionData NVARCHAR(MAX),
    Duration BIGINT NOT NULL,
    FOREIGN KEY (AccountId) REFERENCES Player(AccountId)
);

------------------------------------------------- Adding indexes for optimization
CREATE NONCLUSTERED INDEX IX_Player_Username ON Player (Username);

CREATE NONCLUSTERED INDEX IX_Game_Name_Theme ON Game ([Name], Theme);

CREATE NONCLUSTERED INDEX IX_Game_ProviderId ON Game (ProviderId);

CREATE NONCLUSTERED INDEX IX_CasinoWager_CreatedDateTime ON CasinoWager (CreatedDateTime);

CREATE NONCLUSTERED INDEX IX_CasinoWager_AccountId ON CasinoWager (AccountId);
GO

------------------------------------------------- Creating Stored Procedures
CREATE OR ALTER PROCEDURE sp_InsertCasinoWager
    @WagerId UNIQUEIDENTIFIER,
    @Theme NVARCHAR(100),
    @Provider NVARCHAR(200),
    @GameName NVARCHAR(200),
    @TransactionId UNIQUEIDENTIFIER,
    @BrandId UNIQUEIDENTIFIER,
    @AccountId UNIQUEIDENTIFIER,
    @Username NVARCHAR(100),
    @ExternalReferenceId UNIQUEIDENTIFIER,
    @TransactionTypeId UNIQUEIDENTIFIER,
    @Amount DECIMAL(18, 2),
    @CreatedDateTime DATETIME2(3),
    @NumberOfBets INT,
    @CountryCode NVARCHAR(10),
    @SessionData NVARCHAR(MAX),
    @Duration BIGINT
AS
BEGIN
    BEGIN TRY
        INSERT INTO CasinoWager (
            WagerId, Theme, [Provider], GameName, TransactionId, BrandId, AccountId, Username,
            ExternalReferenceId, TransactionTypeId, Amount, CreatedDateTime, NumberOfBets,
            CountryCode, SessionData, Duration
        )
        VALUES (
            @WagerId, @Theme, @Provider, @GameName, @TransactionId, @BrandId, @AccountId, @Username,
            @ExternalReferenceId, @TransactionTypeId, @Amount, @CreatedDateTime, @NumberOfBets,
            @CountryCode, @SessionData, @Duration
        );
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END;
GO


CREATE OR ALTER PROCEDURE sp_GetCasinoWagersByPlayer
    @AccountId UNIQUEIDENTIFIER,
    @PageSize INT,
    @Page INT
AS
BEGIN
    BEGIN TRY
        -- Calculating total records and total pages
        DECLARE @TotalRecords INT;
        SELECT @TotalRecords = COUNT(*) 
        FROM CasinoWager
        WHERE AccountId = @AccountId;

        DECLARE @TotalPages INT = CEILING(CAST(@TotalRecords AS FLOAT) / @PageSize);

        -- Fetching paginated data, with my descretion for retrieving necessary data 
		-- Rather than retrieving all columns, many of which are unnecessary for the payload
        SELECT 
            WagerId,
            GameName AS [game],
            [Provider] AS [provider],
            Amount,
            CreatedDateTime AS [createdDate]
        FROM CasinoWager
        WHERE AccountId = @AccountId
        ORDER BY CreatedDateTime DESC
        OFFSET (@Page - 1) * @PageSize ROWS
        FETCH NEXT @PageSize ROWS ONLY;

        -- Returning the metadata
        SELECT 
            @Page AS [page],
            @PageSize AS [pageSize],
            @TotalRecords AS [total],
            @TotalPages AS [totalPages];
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END;
GO


CREATE OR ALTER PROCEDURE sp_GetTopSpenders
    @Count INT = 10, --Defaulting to top 10 spenders
    @StartDate DATETIME2 = NULL, --Optional start date filter
    @EndDate DATETIME2 = NULL --Optional end date filter
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        DECLARE @FilterStartDate DATETIME2 = ISNULL(@StartDate, '2024-01-01'); -- NB: This is just for the assessment purpose
        DECLARE @FilterEndDate DATETIME2 = ISNULL(@EndDate, '2030-12-31');     -- incase start and end dates are not filtered, it would just get data using these hardcoded ones

        SELECT TOP (@Count)
            p.AccountId,
            p.Username,
            SUM(cw.Amount) AS TotalAmountSpent
        FROM CasinoWager cw
        INNER JOIN Player p ON cw.AccountId = p.AccountId
        WHERE cw.CreatedDateTime BETWEEN @FilterStartDate AND @FilterEndDate
        GROUP BY p.AccountId, p.Username
        HAVING SUM(cw.Amount) > 0
        ORDER BY TotalAmountSpent DESC;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END;
GO
