-- OrdTfInsertOrder : insert an order and return the new OrderID (EV-ORD-308 step 3-1, 3-2. ShippedDate is NULL)
INSERT INTO Orders
  (CustomerID, EmployeeID, OrderDate, RequiredDate, ShipVia, Freight,
   ShipName, ShipAddress, ShipCity, ShipRegion, ShipPostalCode, ShipCountry)
VALUES
  (@CustomerID, @EmployeeID, @OrderDate, @RequiredDate, @ShipVia, @Freight,
   @ShipName, @ShipAddress, @ShipCity, @ShipRegion, @ShipPostalCode, @ShipCountry);
SELECT CAST(SCOPE_IDENTITY() AS int)
