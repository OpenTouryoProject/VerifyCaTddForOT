-- OrdTfDeleteDetails : delete all lines of the order (EV-ORD-203 step 3. ORD-T8: lines before the order)
DELETE FROM
  [Order Details]
WHERE
  OrderID = @OrderID
