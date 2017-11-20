import subprocess
import re

def freplace(filename, match, new):
    with open(filename) as f:
        ct = f.read()
    ct = re.sub(match, new, ct)
    with open(filename, "w") as f:
        f.write(ct)

def run(cmd, d):
    print(d + "$ " + cmd)
    subprocess.run(args=cmd.split(" "), cwd=d, check=True)

# Switch example order integration to use local projects
freplace("example-order-integration/plugin/example-order-integration.csproj",
         r'<PackageReference Include="BlackMaple.SeedOrders[^>]+>',
         '<ProjectReference Include="../../src/BlackMaple.SeedOrders/BlackMaple.SeedOrders.csproj"/>')
freplace("example-order-integration/tests/tests.csproj",
         r'<PackageReference Include="BlackMaple.SeedOrders[^>]+>',
         '<ProjectReference Include="../../src/BlackMaple.SeedOrders/BlackMaple.SeedOrders.csproj"/>')

# Check if current rev is a tag
curtag = subprocess.check_output(["hg", "id", "-t", "-r", ".^"]).decode("utf-8")

# Build seedorders
if curtag.startswith("seedorders-"):
    ver = curtag.replace("seedorders-", "")
    run("dotnet pack -c Release --include-symbols /p:VersionPrefix=" + ver,
        "src/BlackMaple.SeedOrders")
else:
    run("dotnet build", "src/BlackMaple.SeedOrders")

# Build CSV orders
if curtag.startswith("csv-"):
    # Switch reference to use nuget seedorders
    seedver = subprocess.check_output(["hg", "id", "-t", "-r", "ancestors(.) and tag('re:seedorders')"]).decode("utf-8")
    seedver = seedver.replace("seedorders-", "").split(".")[0]
    freplace("src/BlackMaple.CSVOrders/BlackMaple.CSVOrders.csproj",
            r'<ProjectReference Include="../BlackMaple.SeedOrders[^>]+>',
             '<PackageReference Include="BlackMaple.SeedOrders" Version="' + seedver + '.*"/>')
    freplace("tests/tests.csproj",
            r'<ProjectReference Include="../src/BlackMaple.SeedOrders[^>]+>',
             '<PackageReference Include="BlackMaple.SeedOrders" Version="' + seedver + '.*"/>')
    
    # Pack CSV
    ver = curtag.replace("csv-", "")
    run("dotnet pack -c Release --include-symbols /p:VersionPrefix=" + ver,
        "src/BlackMaple.CSVOrders")
else:
    run("dotnet build", "src/BlackMaple.CSVOrders")

# Build tests and example
run("dotnet build", "tests")
run("dotnet build", "example-order-integration/plugin")
run("dotnet build", "example-order-integration/tests")
