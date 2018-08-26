# storfiler
> Get a file provider api in 30 seconds from configuration.

storfiler is a tiny API to list/download/update/delete files into server folders.

## Installation
Download the [last release](https://github.com/trenoncourt/storfiler/releases), drop it to your server and that's it!

## Configuration
All the configuration can be made in environment variable or appsettings.json file :

### Host
Currently only chose to use IIS integration
```json
"Host": {
  "UseIis": true
}
```

### Kestrel
Kestrel options, see [ms doc](https://docs.microsoft.com/fr-fr/dotnet/api/microsoft.aspnetcore.server.kestrel.core.kestrelserveroptions) for usage
```json
"Kestrel": {
  "AddServerHeader": false
}
```

### Serilog
storfiler use Serilog for logging, see [serilog-settings-configuration](https://github.com/serilog/serilog-settings-configuration) for usage
```json
"Serilog": {
  "MinimumLevel": {
    "Default": "Debug",
    "Override": {
      "Microsoft": "Warning",
      "System": "Warning"
    }
  },
  "WriteTo": [
    { "Name": "Console" }
  ],
  "Enrich": ["FromLogContext"]
}
```

### Storfiler
storfiler configuration
```javascript
"Storfiler": [
  {
    "resource": "docs", // will be accessible at http://host/docs
    "DiskPaths": { // disk provider 
      "Read": ["C:\\documents"] // read multiple folders
    },
    "Methods": [ // declare methods & actions
      {
        "Verb": "Get",
        "Path": "/",
        "Action": "List" // list files
      }
  },
  {
    "Resource": "logs",
    "DiskPaths": {
      "Read": ["C:\\logs"],
      "Write": "C:\\logs"
    },
    "Methods": [
      {
        "Verb": "Get",
        "Path": "/",
        "Action": "List"
      },
      {
        "Verb": "Get",
        "Path": "/{fileName}",
        "Action": "Find" // Find & download one file
      },
      {
        "Verb": "Delete",
        "Path": "/{fileName}",
        "Action": "Remove" // remove one file
      }
    ]
  }
  ]
```
