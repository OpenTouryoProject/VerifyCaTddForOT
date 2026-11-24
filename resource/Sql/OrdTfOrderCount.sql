-- OrdTfOrderCount : does the order exist (EV-ORD-203 step 4: ERR-CONFLICT or ERR-NOTFOUND)
SELECT
  COUNT(*)
FROM
  Orders
WHERE
  OrderID = @OrderID
