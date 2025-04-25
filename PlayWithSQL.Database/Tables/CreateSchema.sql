/* 
   Schema: E-Commerce / Customer Management
   Description: Creates tables for Customer, Product, Order, OrderItem, 
   ZipCode (for US addresses), Address (customer-related), PhoneType, 
   and CustomerPhone.
*/

/* --- Create base tables --- */

/* Customer Table */
CREATE TABLE dbo.Customer
(
    CustomerId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(150) NOT NULL,
    CreatedDate DATETIME2 DEFAULT GETDATE()
);
GO

/* PhoneType lookup table */
CREATE TABLE dbo.PhoneType
(
    PhoneTypeId INT IDENTITY(1,1) PRIMARY KEY,
    PhoneTypeName NVARCHAR(50) NOT NULL  -- Suggested values: 'Mobile', 'Home', 'Work', etc.
);
GO

/* ZipCode Table for US-only addresses */
CREATE TABLE dbo.ZipCode
(
    ZipCode NVARCHAR(10) PRIMARY KEY,
    City NVARCHAR(100) NOT NULL,
    State CHAR(2) NOT NULL  -- State abbreviation (e.g., 'WA', 'CA', etc.)
);
GO

/* Product Table */
CREATE TABLE dbo.ProductCategory
(
    CategoryId INT IDENTITY(1,1) PRIMARY KEY,
    CategoryName NVARCHAR(100) NOT NULL UNIQUE
);
GO

CREATE TABLE dbo.Product
(
    ProductId INT IDENTITY(1,1) PRIMARY KEY,
    ProductName NVARCHAR(150) NOT NULL,
    Description NVARCHAR(MAX),
    Price DECIMAL(10,2) NOT NULL,
    CategoryId INT NOT NULL,
    CONSTRAINT FK_Product_ProductCategory FOREIGN KEY (CategoryId)
        REFERENCES dbo.ProductCategory(CategoryId)
);
GO

/* Order Table */
CREATE TABLE dbo.[Order]
(
    OrderId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    CustomerId UNIQUEIDENTIFIER NOT NULL,
    OrderDate DATETIME2 DEFAULT GETDATE(),
    TotalAmount DECIMAL(10,2) NOT NULL,
    CONSTRAINT FK_Order_Customer FOREIGN KEY (CustomerId)
        REFERENCES dbo.Customer(CustomerId)
);
GO

/* OrderItem Table */
CREATE TABLE dbo.OrderItem
(
    OrderItemId INT IDENTITY(1,1) PRIMARY KEY,
    OrderId UNIQUEIDENTIFIER NOT NULL,
    ProductId INT NOT NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(10,2) NOT NULL,
    CONSTRAINT FK_OrderItem_Order FOREIGN KEY (OrderId)
        REFERENCES dbo.[Order](OrderId),
    CONSTRAINT FK_OrderItem_Product FOREIGN KEY (ProductId)
        REFERENCES dbo.Product(ProductId)
);
GO

/* Address Table */
CREATE TABLE dbo.Address
(
    AddressId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    CustomerId UNIQUEIDENTIFIER NOT NULL,
    StreetAddress NVARCHAR(200) NOT NULL,
    ZipCode NVARCHAR(10) NOT NULL,
    -- Additional fields (e.g., AddressType) can be added as needed
    CONSTRAINT FK_Address_Customer FOREIGN KEY (CustomerId)
        REFERENCES dbo.Customer(CustomerId),
    CONSTRAINT FK_Address_ZipCode FOREIGN KEY (ZipCode)
        REFERENCES dbo.ZipCode(ZipCode)
);
GO

/* CustomerPhone Table */
CREATE TABLE dbo.CustomerPhone
(
    PhoneId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    CustomerId UNIQUEIDENTIFIER NOT NULL,
    PhoneNumber NVARCHAR(20) NOT NULL,
    PhoneTypeId INT NOT NULL,
    CONSTRAINT FK_CustomerPhone_Customer FOREIGN KEY (CustomerId)
        REFERENCES dbo.Customer(CustomerId),
    CONSTRAINT FK_CustomerPhone_PhoneType FOREIGN KEY (PhoneTypeId)
        REFERENCES dbo.PhoneType(PhoneTypeId)
);
GO

/* --- Create Indexes --- */

CREATE INDEX IX_Order_CustomerId
ON dbo.[Order](CustomerId);
GO

CREATE INDEX IX_OrderItem_OrderId
ON dbo.OrderItem(OrderId);
GO

CREATE INDEX IX_Address_CustomerId
ON dbo.Address(CustomerId);
GO

CREATE INDEX IX_CustomerPhone_CustomerId
ON dbo.CustomerPhone(CustomerId);
GO

CREATE NONCLUSTERED INDEX IX_Customer_Email
ON dbo.Customer(Email);
GO