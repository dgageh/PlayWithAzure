SELECT ProductId, ProductName, Description, Price, CategoryName from dbo.Product
	LEFT JOIN dbo.ProductCategory on Product.CategoryId = ProductCategory.CategoryId
	WHERE ProductId = 1

SELECT * from Customer
	LEFT JOIN Address on Customer.CustomerId = Address.CustomerId
	left join CustomerPhone on Customer.CustomerId = CustomerPhone.CustomerId
	left join PhoneType on CustomerPhone.PhoneTypeId = PhoneType.PhoneTypeId
	left join [Order] on Customer.CustomerId = [Order].Customerid
	left join OrderItem on OrderItem.OrderId = [Order].orderid

select ProductName From OrderItem
	join Product on OrderItem.ProductId = Product.ProductId


SELECT AddressId, StreetAddress, Address.ZipCode, City, [State] FROM dbo.Address
	JOIN ZipCode on Address.ZipCode = ZipCode.ZipCode

--DELETE FROM customer where CustomerId = 1

--CREATE USER FakeCustomerFunctionApp FROM EXTERNAL PROVIDER;
--ALTER ROLE db_datareader ADD MEMBER FakeCustomersFunctionApp;
--ALTER ROLE db_datawriter ADD MEMBER FakeCustomersFunctionApp;

--GRANT EXECUTE ON dbo.InsertProduct TO FakeCustomersFunctionApp;
--GRANT EXECUTE ON dbo.[AddCustomerAddress]TO FakeCustomersFunctionApp;
--GRANT EXECUTE ON dbo.[AddCustomerPhone]TO FakeCustomersFunctionApp;
--GRANT EXECUTE ON dbo.[AddCustomerOrder]TO FakeCustomersFunctionApp;
--GRANT EXECUTE ON dbo.[AddOrderItem]TO FakeCustomersFunctionApp;
--GRANT EXECUTE ON dbo.[InsertCustomer]TO FakeCustomersFunctionApp;

select count(*) from Customer
select count(*) from dbo.[Order]
select count(*) from Address
select count(*) from OrderItem
select count(*) from Product
select count(*) from ProductCategory
select count(*) from CustomerPhone
select count(*) from PhoneType

select distinct state, count(address.addressid) from ZipCode
	join dbo.Address on ZipCode.ZipCode = Address.ZipCode
	group by ZipCode.State
	order by count(address.addressid) desc


SELECT OrderItemId, OrderItem.ProductId, Quantity, UnitPrice, ProductName, CategoryName, Description FROM dbo.OrderItem 
	JOIN dbo.Product ON OrderItem.ProductId = Product.ProductId
	JOIN dbo.ProductCategory on Product.CategoryId = ProductCategory.CategoryId 
	WHERE OrderId=5