-- OrdTfShipClearOrder : set ShippedDate to NULL by key and RowVersion, RowVersion + 1. ShipVia and Freight are not changed (EV-ORD-403 step 6)
UPDATE
  Orders
SET
  ShippedDate = NULL,
  RowVersion = RowVersion + 1
WHERE
  OrderID = @OrderID
  AND RowVersion = @RowVersion
