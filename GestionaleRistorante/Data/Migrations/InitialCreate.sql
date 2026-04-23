IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [Dishes] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(150) NOT NULL,
    [Description] nvarchar(2000) NULL,
    [Price] decimal(18,2) NOT NULL,
    [IsAvailable] bit NOT NULL,
    [ImageUrl] nvarchar(1000) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_Dishes] PRIMARY KEY ([Id])
);

CREATE TABLE [Ingredients] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(150) NOT NULL,
    [QuantityInStock] decimal(18,3) NOT NULL,
    [Unit] nvarchar(30) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_Ingredients] PRIMARY KEY ([Id])
);

CREATE TABLE [Roles] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(50) NOT NULL,
    CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
);

CREATE TABLE [Tables] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Capacity] int NOT NULL,
    [Status] nvarchar(30) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_Tables] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Tables_Capacity] CHECK ([Capacity] > 0)
);

CREATE TABLE [DishIngredients] (
    [Id] int NOT NULL IDENTITY,
    [DishId] int NOT NULL,
    [IngredientId] int NOT NULL,
    [Quantity] decimal(18,3) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_DishIngredients] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_DishIngredients_Dishes_DishId] FOREIGN KEY ([DishId]) REFERENCES [Dishes] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_DishIngredients_Ingredients_IngredientId] FOREIGN KEY ([IngredientId]) REFERENCES [Ingredients] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Users] (
    [Id] int NOT NULL IDENTITY,
    [Email] nvarchar(256) NOT NULL,
    [PasswordHash] nvarchar(512) NOT NULL,
    [FirstName] nvarchar(100) NOT NULL,
    [LastName] nvarchar(100) NOT NULL,
    [RoleId] int NOT NULL,
    [RequirePasswordChange] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Users_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Orders] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NULL,
    [TableId] int NULL,
    [CreatedByRoleId] int NOT NULL,
    [Status] nvarchar(50) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_Orders] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Orders_Status] CHECK ([Status] IN ('pending','in_preparation','ready','served','cancelled')),
    CONSTRAINT [FK_Orders_Tables_TableId] FOREIGN KEY ([TableId]) REFERENCES [Tables] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Orders_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [RefreshTokens] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [TokenHash] nvarchar(128) NOT NULL,
    [ExpiresAtUtc] datetime2 NOT NULL,
    [IsRevoked] bit NOT NULL,
    [RevokedAtUtc] datetime2 NULL,
    [ReplacedByTokenHash] nvarchar(max) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RefreshTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Reservations] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [TableId] int NOT NULL,
    [ReservationAtUtc] datetime2 NOT NULL,
    [PartySize] int NOT NULL,
    [Notes] nvarchar(1000) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_Reservations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Reservations_Tables_TableId] FOREIGN KEY ([TableId]) REFERENCES [Tables] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Reservations_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [OrderItems] (
    [Id] int NOT NULL IDENTITY,
    [OrderId] int NOT NULL,
    [DishId] int NOT NULL,
    [Quantity] int NOT NULL,
    [UnitPrice] decimal(18,2) NOT NULL,
    [Notes] nvarchar(1000) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_OrderItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_OrderItems_Dishes_DishId] FOREIGN KEY ([DishId]) REFERENCES [Dishes] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_OrderItems_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE CASCADE
);

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Name') AND [object_id] = OBJECT_ID(N'[Roles]'))
    SET IDENTITY_INSERT [Roles] ON;
INSERT INTO [Roles] ([Id], [Name])
VALUES (1, N'Customer'),
(2, N'Kitchen'),
(3, N'Sala'),
(99, N'Admin');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Name') AND [object_id] = OBJECT_ID(N'[Roles]'))
    SET IDENTITY_INSERT [Roles] OFF;

CREATE INDEX [IX_Dishes_IsDeleted] ON [Dishes] ([IsDeleted]);

CREATE INDEX [IX_Dishes_Name] ON [Dishes] ([Name]);

CREATE UNIQUE INDEX [IX_DishIngredients_DishId_IngredientId] ON [DishIngredients] ([DishId], [IngredientId]);

CREATE INDEX [IX_DishIngredients_IngredientId] ON [DishIngredients] ([IngredientId]);

CREATE INDEX [IX_Ingredients_IsDeleted] ON [Ingredients] ([IsDeleted]);

CREATE UNIQUE INDEX [IX_Ingredients_Name] ON [Ingredients] ([Name]);

CREATE INDEX [IX_OrderItems_DishId] ON [OrderItems] ([DishId]);

CREATE INDEX [IX_OrderItems_OrderId] ON [OrderItems] ([OrderId]);

CREATE INDEX [IX_Orders_CreatedAt] ON [Orders] ([CreatedAt]);

CREATE INDEX [IX_Orders_Status] ON [Orders] ([Status]);

CREATE INDEX [IX_Orders_TableId] ON [Orders] ([TableId]);

CREATE INDEX [IX_Orders_UserId] ON [Orders] ([UserId]);

CREATE UNIQUE INDEX [IX_RefreshTokens_TokenHash] ON [RefreshTokens] ([TokenHash]);

CREATE INDEX [IX_RefreshTokens_UserId_IsRevoked] ON [RefreshTokens] ([UserId], [IsRevoked]);

CREATE INDEX [IX_Reservations_ReservationAtUtc] ON [Reservations] ([ReservationAtUtc]);

CREATE INDEX [IX_Reservations_TableId] ON [Reservations] ([TableId]);

CREATE INDEX [IX_Reservations_UserId] ON [Reservations] ([UserId]);

CREATE UNIQUE INDEX [IX_Tables_Name] ON [Tables] ([Name]);

CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);

CREATE INDEX [IX_Users_RoleId] ON [Users] ([RoleId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260420104738_InitialCreate', N'10.0.6');

COMMIT;
GO

