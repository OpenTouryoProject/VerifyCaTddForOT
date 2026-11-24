-- OrdTfExistsCustomer : does the customer exist (VAL-EXISTS)
SELECT
  COUNT(*)
FROM
  Customers
WHERE
  CustomerID = @CustomerID
