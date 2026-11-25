-- OrdTfShipLockOrder : get the order by key and RowVersion with an update lock (EV-ORD-402 step 3, EV-ORD-403 step 3)
SELECT
  ShippedDate
FROM
  Orders WITH (UPDLOCK, ROWLOCK)
WHERE
  OrderID = @OrderID
  AND RowVersion = @RowVersion
