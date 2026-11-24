-- OrdTfEditHeader : one order for edit with customer info (EV-ORD-301 step 3-1, 3-3, 4)
SELECT
  o.OrderID,
  o.CustomerID,
  o.EmployeeID,
  o.ShipVia,
  c.CompanyName AS CustomerName,
  c.Phone AS CustomerPhone,
  c.Address AS CustomerAddress,
  c.City AS CustomerCity,
  c.Region AS CustomerRegion,
  c.PostalCode AS CustomerPostalCode,
  c.Country AS CustomerCountry,
  o.OrderDate,
  o.RequiredDate,
  o.ShippedDate,
  o.ShipName,
  o.ShipAddress,
  o.ShipCity,
  o.ShipRegion,
  o.ShipPostalCode,
  o.ShipCountry,
  o.Freight,
  o.RowVersion
FROM
  Orders o
  LEFT JOIN Customers c ON c.CustomerID = o.CustomerID
WHERE
  o.OrderID = @OrderID
