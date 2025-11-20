using System.Globalization;

namespace ObjectPrinting.Tests
{
    [TestFixture]
    public class ObjectPrinterAcceptanceTests
    {
        [Test]
        public void Demo()
        {
            var person = new Person { Name = "Alex", Age = 19 };

            var printer = ObjectPrinter.For<Person>()
                .Excluding<Guid>() // 1. Исключить из сериализации свойства определенного типа                      
                .Printing<int>().Using(i => $"Int({i})") // 2. Указать альтернативный способ сериализации для определенного типа    
                .Printing<double>().Using(CultureInfo.InvariantCulture) // 3. Для числовых типов указать культуру
                .Printing(p => p.Name).Using(n => $"very cool name: {n}") // 4. Настроить сериализацию конкретного свойства
                .Printing(p => p.Name).TrimToLength(10) // 5. Настроить обрезание строковых свойств
                .Excluding(p => p.Age); // 6. Исключить конкретное свойство

            var s1 = printer.PrintToString(person);
        
            var s2 = person.PrintToString(); // 7. Синтаксический сахар — метод расширения
        
            var s3 = person.PrintToString(p => p
                    .Printing<double>().Using(CultureInfo.GetCultureInfo("ru-RU")) // 8. ...с конфигурированием
            );
        }
    }
}