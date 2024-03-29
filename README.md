# Mushroom Log Additions

Framework for adding new tree type -> mushroom log output mappings.

## Format

The content pack format for this framework is extremely simple: `Dictionary<string,string>`.

```json
{
	"treeType#": "outputQualifiedItemId"
}
```

Example:
```json
{
	// mushroom tree -> mushroom tree seed
	"7": "(O)891"
}
```