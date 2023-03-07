# Set variables for the resource group and Cosmos DB instance
$resourceGroupName = "OrleansPubCrawl"
$location = "westeurope"
$accountName = "orleanspubcrawl"
$databaseName = "orleans"
$grainStateCollectionName = "grainstate"
$leaseCollectionName = "leases"

# Create a new resource group
az group create `
    --name $resourceGroupName `
    --location $location

# Create a new Cosmos DB instance with the SQL API
az cosmosdb create `
    --name $accountName `
    --resource-group $resourceGroupName `
    --kind GlobalDocumentDB `
    --default-consistency-level Eventual `
    --capabilities EnableServerless

# Create the database
az cosmosdb sql database create `
    --account-name $accountName `
    --name $databaseName `
    --resource-group $resourceGroupName

# Create the grainstate collection
az cosmosdb sql container create `
    --account-name $accountName `
    --database-name $databaseName `
    --name $grainStateCollectionName `
    --partition-key-path "/id" `
    --resource-group $resourceGroupName

# Create the lease collection
az cosmosdb sql container create `
    --account-name $accountName `
    --database-name $databaseName `
    --name $leaseCollectionName `
    --partition-key-path "/id" `
    --resource-group $resourceGroupName

# Get the connection string for the Cosmos DB account
$cosmosConnectionString = az cosmosdb keys list `
    --name $accountName `
    --resource-group $resourceGroupName `
    --type connection-strings `
    --query "connectionStrings[0].connectionString" `
    --output tsv

# Create a storage account
az storage account create `
    --name orleanspubcrawl `
    --resource-group $resourceGroupName `
    --location $location `
    --sku Standard_LRS

# Create a table in the storage account
az storage table create `
    --name grainstate `
    --account-name orleanspubcrawl

# Get storage connection string
$storageConnectionString = az storage account show-connection-string `
    --name orleanspubcrawl `
    --resource-group $resourceGroupName `
    --query "connectionString" `
    --output tsv

# Output the connection string
Write-Host "Cosmos Connection String: $cosmosConnectionString"
Write-Host "Storage Connection String: $storageConnectionString"
