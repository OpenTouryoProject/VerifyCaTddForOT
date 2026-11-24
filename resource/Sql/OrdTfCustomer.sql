-- OrdTfCustomer : one customer (EV-ORD-302 step 2)
SELECT
  CompanyName, Phone, Address, City, Region, PostalCode, Country
FROM
  Customers
WHERE
  CustomerID = @CustomerID
