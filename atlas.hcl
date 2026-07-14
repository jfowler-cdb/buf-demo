data "external_schema" "ef" {
  program = [
    "sh", "-c", "$HOME/.dotnet/tools/atlas-ef",
  ]
  working_dir = "API"
}

env "local" {
  src = data.external_schema.ef.url
  dev = "sqlite://dev?mode=memory"
  url = "sqlite://API/releases.db"
  migration {
    dir = "file://migrations"
  }
}
