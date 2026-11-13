-- OrdTfEmployees : employee choices for SC-ORD-01 (LastName, FirstName ascending)
SELECT
  EmployeeID, LastName, FirstName
FROM
  Employees
ORDER BY
  LastName ASC, FirstName ASC, EmployeeID ASC
