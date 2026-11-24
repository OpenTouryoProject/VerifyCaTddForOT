-- OrdTfDetailHeader : one order with customer, employee and shipper (EV-ORD-201 steps 1-2)
SELECT
  o.OrderID,
  c.CompanyName AS CustomerName,
  c.Phone AS CustomerPhone,
  c.Address AS CustomerAddress,
  c.City AS CustomerCity,
  c.Region AS CustomerRegion,
  c.PostalCode AS CustomerPostalCode,
  c.Country AS CustomerCountry,
  e.LastName,
  e.FirstName,
  o.OrderDate,
  o.RequiredDate,
  o.ShippedDate,
  s.CompanyName AS ShipperName,
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
  LEFT JOIN Employees e ON e.EmployeeID = o.EmployeeID
  LEFT JOIN Shippers s ON s.ShipperID = o.ShipVia
WHERE
  o.OrderID = @OrderID
