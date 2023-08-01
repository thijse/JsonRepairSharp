# JsonRepair Sharp

Repair invalid JSON documents. This C# library is a functionally equivalent to the TypeScript jsonrepair library, see https://github.com/josdejong/jsonrepair

Use it in a full-fledged application: https://jsoneditoronline.org

Read the background article ["How to fix JSON and validate it with ease"](https://jsoneditoronline.org/indepth/parse/fix-json/)

The following issues can be fixed:

- Add missing quotes around keys
- Add missing escape characters
- Add missing commas
- Add missing closing brackets
- Replace single quotes with double quotes
- Replace special quote characters like `“...”`  with regular double quotes
- Replace special white space characters with regular spaces
- Replace Python constants `None`, `True`, and `False` with `null`, `true`, and `false`
- Strip trailing commas
- Strip comments like `/* ... */` and `// ...`
- Strip JSONP notation like `callback({ ... })`
- Strip escape characters from an escaped string like `{\"stringified\": \"content\"}`
- Strip MongoDB data types like `NumberLong(2)` and `ISODate("2012-12-19T06:01:17.171Z")`
- Concatenate strings like `"long text" + "more text on next line"`
- Turn newline delimited JSON into a valid JSON array, for example:
    ```
    { "id": 1, "name": "John" }
    { "id": 2, "name": "Sarah" }
    ```


## Use


```cs
 try
 {
     // The following is invalid JSON: is consists of JSON contents copied from 
     // a JavaScript code base, where the keys are missing double quotes, 
     // and strings are using single quotes:
     string json = "{name: 'John'}";
     string repaired = JSONRepair.JsonRepair(json);
     Console.WriteLine(repaired);
     // Output: {"name": "John"}
 }
 catch (JSONRepairError err)
 {
     Console.WriteLine(err.Message);
     Console.WriteLine("Position: " + err.Data["Position"]);
 }
```

## Alternatives:

Similar libraries:

- https://github.com/josdejong/jsonrepair
- https://github.com/RyanMarcus/dirty-json

## License

Released under the [ISC license](LICENSE.md).
