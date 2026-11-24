-- OrdTfExistsEmployee : does the employee exist (VAL-EXISTS)
SELECT
  COUNT(*)
FROM
  Employees
WHERE
  EmployeeID = @EmployeeID
