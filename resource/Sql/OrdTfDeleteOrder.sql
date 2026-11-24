-- OrdTfDeleteOrder : delete the order by key and RowVersion (EV-ORD-203 step 4. RowVersion is not incremented on delete)
DELETE FROM
  Orders
WHERE
  OrderID = @OrderID
  AND RowVersion = @RowVersion
