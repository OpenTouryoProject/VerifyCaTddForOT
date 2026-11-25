-- OrdTfShipUpdateOrder : set the shipping information by key and RowVersion, RowVersion + 1 (EV-ORD-402 step 6)
UPDATE
  Orders
SET
  ShippedDate = @ShippedDate,
  ShipVia = @ShipVia,
  Freight = @Freight,
  RowVersion = RowVersion + 1
WHERE
  OrderID = @OrderID
  AND RowVersion = @RowVersion
