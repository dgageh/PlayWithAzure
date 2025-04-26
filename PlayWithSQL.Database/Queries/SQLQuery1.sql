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