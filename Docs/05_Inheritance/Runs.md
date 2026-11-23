# Шпаргалка лектора: Глава 5. Спадкування (Inheritance)

> **Фокус глави:** як EF Core відображає ООП-ієрархії (`User` → `Employee`, `Manager`) на реляційні таблиці. Рекомендується запускати всі 3 уроки підряд і порівнювати 2 речі в кожному виводі: **DDL таблиць із `sqlite_master`** та **згенерований SQL (`ToQueryString()`)**.

## План проведення та запуски

| ID | Тема та конспект | Про що говорити (ключові тези) | Команда запуску | На що звернути увагу у виводі |
|---|---|---|---|---|
| **5.1** | [TPH — Table Per Hierarchy](5_01_TphInheritance.md) · [код](../../src/05_Inheritance/Lesson_5_01_TphInheritance.cs) | • **Стратегія за замовчуванням** в EF Core: уся ієрархія в **одній таблиці**.<br>• Службовий стовпець `Discriminator` (можна налаштувати через `HasDiscriminator`).<br>• **Плюси:** найшвидші запити (без `JOIN` і `UNION ALL`).<br>• **Мінуси:** багато `NULL`-значень у стовпцях підкласів (не можна поставити `NOT NULL` на специфічні поля похідних типів). | `dotnet run --project src/EFCoreMastery.ConsoleApp -- 5.1` | • У `sqlite_master` лише **1 таблиця** з `Discriminator`.<br>• У `ToQueryString()` для `db.Employees` видно `WHERE "u"."Discriminator" = 'Employee'`. |
| **5.2** | [TPT — Table Per Type](5_02_TptInheritance.md) · [код](../../src/05_Inheritance/Lesson_5_02_TptInheritance.cs) | • Вмикається через `UseTptMappingStrategy()` (або окремі `ToTable(...)` для кожного класу).<br>• Для базового і кожного похідного класу — **своя нормалізована таблиця**, пов'язана 1:1 по спільному `Id`.<br>• **Плюси:** ідеальна нормалізація, немає зайвих `NULL`.<br>• **Мінуси:** найповільніші поліморфні запити (багато `LEFT JOIN`). | `dotnet run --project src/EFCoreMastery.ConsoleApp -- 5.2` | • У `sqlite_master` **3 таблиці** (`Users`, `Employees`, `Managers`) із FK `Id -> Users(Id)`.<br>• У `ToQueryString()` для `db.Users` видно `LEFT JOIN` до кожної похідної таблиці. |
| **5.3** | [TPC — Table Per Class](5_03_TpcInheritance.md) · [код](../../src/05_Inheritance/Lesson_5_03_TpcInheritance.cs) | • Вмикається через `UseTpcMappingStrategy()` (EF Core 7+).<br>• Кожен конкретний тип отримує **повну самостійну таблицю** з усіма успадкованими стовпцями.<br>• **Плюси:** швидкі запити до конкретного підтипу (`db.Employees`), немає `JOIN`.<br>• **Мінуси:** поліморфний запит `db.Users` робить `UNION ALL`; потрібна координація генерації ключів (у SQL Server — `SEQUENCE`, у SQLite — явні/унікальні ID). | `dotnet run --project src/EFCoreMastery.ConsoleApp -- 5.3` | • У `sqlite_master` **незалежні таблиці** без FK на базову.<br>• У `ToQueryString()` для поліморфної вибірки `db.Users` видно `UNION ALL`. |

---

## Порівняльна таблиця стратегій (для підсумку глави)

| Критерій | TPH (за замовчуванням) | TPT | TPC |
|---|---|---|---|
| **Кількість таблиць** | 1 на всю ієрархію | По 1 на кожен клас (базовий + похідні) | По 1 на кожен конкретний (неабстрактний) клас |
| **Поліморфний `SELECT`** | Швидкий (1 таблиця) | Повільний (`LEFT JOIN`) | Середній (`UNION ALL`) |
| **`SELECT` конкретного типу** | Швидкий (`WHERE Discriminator`) | Середній (`INNER JOIN`) | Найшвидший (1 таблиця без фільтра) |
| **Проблема `NULL` у стовпцях** | Є | Немає | Немає |
| **Коли обирати** | У 90% випадків за замовчуванням | Сувора нормалізація БД / legacy-схема | Рідкі поліморфні запити, часті вибірки конкретних типів |
