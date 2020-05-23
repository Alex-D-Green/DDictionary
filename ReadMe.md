# DDictionary

This is a simple WPF based program to manage the personal dictionary for those who learns a foreign language.

The key features of the program is:
  - Customizable relations between the words (for better memorizing);
  - Advanced filtration & search;
  - Ranging words by groups depending on how well they are known;
  - Import/Export through the CSV format;
  - Trainings to memorize the words.


## Technical details

The program is implemented on .Net framework, user interface is made on WPF, 
as a data storage is used [SQLite data base](https://system.data.sqlite.org/index.html/doc/trunk/www/index.wiki) 
and [Dapper](https://github.com/StackExchange/Dapper). [CsvHelper](https://joshclose.github.io/CsvHelper/) and 
[Reactive Extensions for .NET](https://github.com/dotnet/reactive) are used to work with CSV format.


## License

See the [LICENSE](LICENSE) file for license rights and limitations (Apache 2.0).

Icons from [https://freeicons.io/](https://freeicons.io/)