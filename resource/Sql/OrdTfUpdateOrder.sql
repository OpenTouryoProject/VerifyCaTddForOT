-- OrdTfUpdateOrder : update an order by key and RowVersion, RowVersion + 1 (EV-ORD-308 step 4-1. ShippedDate is not updated)
UPDATE
  Orders
SET
  CustomerID = @CustomerID,
  EmployeeID = @EmployeeID,
  OrderDate = @OrderDate,
  RequiredDate = @RequiredDate,
  ShipVia = @ShipVia,
  Freight = @Freight,
  ShipName = @ShipName,
  ShipAddress = @ShipAddress,
  ShipCity = @ShipCity,
  ShipRegion = @ShipRegion,
  ShipPostalCode = @ShipPostalCode,
  ShipCountry = @ShipCountry,
  RowVersion = RowVersion + 1
WHERE
  OrderID = @OrderID
  AND RowVersion = @RowVersion
