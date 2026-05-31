$connString = "Server=sql1004.site4now.net;Database=db_ac99f8_pbl3;User Id=db_ac99f8_pbl3_admin;Password=abc1234@;TrustServerCertificate=True;MultipleActiveResultSets=true;"
$connection = New-Object System.Data.SqlClient.SqlConnection($connString)
$connection.Open()

$query = "SELECT * FROM UserSystem.SystemConfiguration"
$cmd = New-Object System.Data.SqlClient.SqlCommand($query, $connection)
$adapter = New-Object System.Data.SqlClient.SqlDataAdapter($cmd)
$dt = New-Object System.Data.DataTable
$adapter.Fill($dt) | Out-Null
$dt | Format-List

$connection.Close()
