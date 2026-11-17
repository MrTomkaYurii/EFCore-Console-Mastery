# EFCore-Console-Mastery

Навчальний .NET-солушен (**C# / .NET 11 / EF Core 11 / SQLite**), який повністю переносить курс [Entity Framework Core на METANIT.COM](https://metanit.com/sharp/efcore/) у формат:
- **окремих проєктів для кожної глави**;
- **самодостатніх C#-прикладів** на базі **SQLite** (запускаються «з коробки» без зовнішнього сервера БД) з **детальними українськими коментарями**;
- **жорстко структурованої української документації (`.md`)** до кожного окремого параграфа.

---

## Архітектура солушена

```text
EFCore-Console-Mastery/
├── EFCore-Console-Mastery.slnx                  # Головний файл рішення (10 проєктів)
├── README.md                                    # Навігація по курсу
├── src/
│   ├── EFCoreMastery.Core/                      # Спільні інтерфейси (ILesson) та SqliteDbHelper
│   ├── EFCoreMastery.ConsoleApp/                # Стартовий консольний проєкт з інтерактивним меню
│   ├── 01_Introduction/                         # Глава 1 (8 тем)
│   ├── 02_DatabaseProviders/                    # Глава 2 (3 теми)
│   ├── 03_ModelsAndMapping/                     # Глава 3 (13 тем)
│   ├── 04_Relations/                            # Глава 4 (12 тем)
│   ├── 05_Inheritance/                          # Глава 5 (3 теми)
│   ├── 06_QueriesAndLinq/                       # Глава 6 (11 тем)
│   ├── 07_SqlAndStoredProcedures/               # Глава 7 (3 теми)
│   └── 08_AdvancedTopics/                       # Глава 8 (5 тем)
└── Docs/
    ├── 01_Introduction/                         # Markdown-конспекти Глави 1
    ├── 02_DatabaseProviders/                    # Markdown-конспекти Глави 2
    ├── 03_ModelsAndMapping/                     # Markdown-конспекти Глави 3
    ├── 04_Relations/                            # Markdown-конспекти Глави 4
    ├── 05_Inheritance/                          # Markdown-конспекти Глави 5
    ├── 06_QueriesAndLinq/                       # Markdown-конспекти Глави 6
    ├── 07_SqlAndStoredProcedures/               # Markdown-конспекти Глави 7
    └── 08_AdvancedTopics/                       # Markdown-конспекти Глави 8
```

---

## Швидкий старт

```powershell
# Збірка всього солушена
dotnet build EFCore-Console-Mastery.slnx

# Інтерактивне меню (вибір глави та уроку)
dotnet run --project src/EFCoreMastery.ConsoleApp

# Запуск конкретного параграфа за ID (наприклад, 1.4 або 3.6)
dotnet run --project src/EFCoreMastery.ConsoleApp -- 1.4

# Наскрізний запуск усіх 58 уроків
dotnet run --project src/EFCoreMastery.ConsoleApp -- --all
```

---

## Повний зміст курсу (8 глав, 58 параграфів)

### Глава 1. Вступ до Entity Framework Core (`01_Introduction`)
| ID | Тема | Код | Конспект |
|---|---|---|---|
| 1.1 | Що таке Entity Framework Core | [Lesson_1_1_WhatIsEfCore.cs](src/01_Introduction/Lesson_1_1_WhatIsEfCore.cs) | [1_1_WhatIsEfCore.md](Docs/01_Introduction/1_1_WhatIsEfCore.md) |
| 1.2 | Перший застосунок на EF Core | [Lesson_1_2_FirstApp.cs](src/01_Introduction/Lesson_1_2_FirstApp.cs) | [1_2_FirstApp.md](Docs/01_Introduction/1_2_FirstApp.md) |
| 1.3 | Підключення до існуючої бази даних | [Lesson_1_3_ExistingDatabase.cs](src/01_Introduction/Lesson_1_3_ExistingDatabase.cs) | [1_3_ExistingDatabase.md](Docs/01_Introduction/1_3_ExistingDatabase.md) |
| 2.16 | Управління базою даних | [Lesson_2_16_DatabaseManagement.cs](src/01_Introduction/Lesson_2_16_DatabaseManagement.cs) | [2_16_DatabaseManagement.md](Docs/01_Introduction/2_16_DatabaseManagement.md) |
| 1.4 | Основні операції з даними. CRUD | [Lesson_1_4_CrudOperations.cs](src/01_Introduction/Lesson_1_4_CrudOperations.cs) | [1_4_CrudOperations.md](Docs/01_Introduction/1_4_CrudOperations.md) |
| 1.5 | Конфігурація підключення | [Lesson_1_5_ConnectionConfiguration.cs](src/01_Introduction/Lesson_1_5_ConnectionConfiguration.cs) | [1_5_ConnectionConfiguration.md](Docs/01_Introduction/1_5_ConnectionConfiguration.md) |
| 1.6 | Логування операцій | [Lesson_1_6_Logging.cs](src/01_Introduction/Lesson_1_6_Logging.cs) | [1_6_Logging.md](Docs/01_Introduction/1_6_Logging.md) |
| 2.2 | Управління схемою БД та міграції | [Lesson_2_2_Migrations.cs](src/01_Introduction/Lesson_2_2_Migrations.cs) | [2_2_Migrations.md](Docs/01_Introduction/2_2_Migrations.md) |

### Глава 2. Провайдери баз даних (`02_DatabaseProviders`)
| ID | Тема | Код | Конспект |
|---|---|---|---|
| 7.1 | Провайдер MS SQL Server | [Lesson_7_1_SqlServerProvider.cs](src/02_DatabaseProviders/Lesson_7_1_SqlServerProvider.cs) | [7_1_SqlServerProvider.md](Docs/02_DatabaseProviders/7_1_SqlServerProvider.md) |
| 7.2 | Провайдер MySQL | [Lesson_7_2_MySqlProvider.cs](src/02_DatabaseProviders/Lesson_7_2_MySqlProvider.cs) | [7_2_MySqlProvider.md](Docs/02_DatabaseProviders/7_2_MySqlProvider.md) |
| 7.3 | Провайдер PostgreSQL | [Lesson_7_3_PostgreSqlProvider.cs](src/02_DatabaseProviders/Lesson_7_3_PostgreSqlProvider.cs) | [7_3_PostgreSqlProvider.md](Docs/02_DatabaseProviders/7_3_PostgreSqlProvider.md) |

### Глава 3. Створення моделей в EF Core (`03_ModelsAndMapping`)
| ID | Тема | Код | Конспект |
|---|---|---|---|
| 2.1 | Моделі, Fluent API та анотації даних | [Lesson_2_1_ModelsAndFluentApi.cs](src/03_ModelsAndMapping/Lesson_2_1_ModelsAndFluentApi.cs) | [2_1_ModelsAndFluentApi.md](Docs/03_ModelsAndMapping/2_1_ModelsAndFluentApi.md) |
| 2.3 | Визначення моделей | [Lesson_2_3_DefiningModels.cs](src/03_ModelsAndMapping/Lesson_2_3_DefiningModels.cs) | [2_3_DefiningModels.md](Docs/03_ModelsAndMapping/2_3_DefiningModels.md) |
| 2.4 | Властивості сутності | [Lesson_2_4_EntityProperties.cs](src/03_ModelsAndMapping/Lesson_2_4_EntityProperties.cs) | [2_4_EntityProperties.md](Docs/03_ModelsAndMapping/2_4_EntityProperties.md) |
| 2.10 | Конструктори сутностей | [Lesson_2_10_EntityConstructors.cs](src/03_ModelsAndMapping/Lesson_2_10_EntityConstructors.cs) | [2_10_EntityConstructors.md](Docs/03_ModelsAndMapping/2_10_EntityConstructors.md) |
| 2.5 | Використання полів сутності | [Lesson_2_5_EntityFields.cs](src/03_ModelsAndMapping/Lesson_2_5_EntityFields.cs) | [2_5_EntityFields.md](Docs/03_ModelsAndMapping/2_5_EntityFields.md) |
| 2.6 | Зіставлення таблиць і стовпців | [Lesson_2_6_TablesAndColumns.cs](src/03_ModelsAndMapping/Lesson_2_6_TablesAndColumns.cs) | [2_6_TablesAndColumns.md](Docs/03_ModelsAndMapping/2_6_TablesAndColumns.md) |
| 2.15 | Обов'язкові та необов'язкові властивості | [Lesson_2_15_RequiredAndOptional.cs](src/03_ModelsAndMapping/Lesson_2_15_RequiredAndOptional.cs) | [2_15_RequiredAndOptional.md](Docs/03_ModelsAndMapping/2_15_RequiredAndOptional.md) |
| 2.7 | Налаштування ключів | [Lesson_2_7_Keys.cs](src/03_ModelsAndMapping/Lesson_2_7_Keys.cs) | [2_7_Keys.md](Docs/03_ModelsAndMapping/2_7_Keys.md) |
| 2.17 | Налаштування індексів | [Lesson_2_17_Indexes.cs](src/03_ModelsAndMapping/Lesson_2_17_Indexes.cs) | [2_17_Indexes.md](Docs/03_ModelsAndMapping/2_17_Indexes.md) |
| 2.8 | Генерація значень властивостей і стовпців | [Lesson_2_8_ValueGeneration.cs](src/03_ModelsAndMapping/Lesson_2_8_ValueGeneration.cs) | [2_8_ValueGeneration.md](Docs/03_ModelsAndMapping/2_8_ValueGeneration.md) |
| 2.9 | Обмеження властивостей | [Lesson_2_9_PropertyConstraints.cs](src/03_ModelsAndMapping/Lesson_2_9_PropertyConstraints.cs) | [2_9_PropertyConstraints.md](Docs/03_ModelsAndMapping/2_9_PropertyConstraints.md) |
| 2.13 | Конфігурація моделей (`IEntityTypeConfiguration`) | [Lesson_2_13_ModelConfiguration.cs](src/03_ModelsAndMapping/Lesson_2_13_ModelConfiguration.cs) | [2_13_ModelConfiguration.md](Docs/03_ModelsAndMapping/2_13_ModelConfiguration.md) |
| 2.14 | Ініціалізація БД початковими даними | [Lesson_2_14_DataSeeding.cs](src/03_ModelsAndMapping/Lesson_2_14_DataSeeding.cs) | [2_14_DataSeeding.md](Docs/03_ModelsAndMapping/2_14_DataSeeding.md) |

### Глава 4. Відносини між моделями (`04_Relations`)
| ID | Тема | Код | Конспект |
|---|---|---|---|
| 3.1 | Зовнішні ключі та навігаційні властивості | [Lesson_3_1_ForeignKeysAndNavigation.cs](src/04_Relations/Lesson_3_1_ForeignKeysAndNavigation.cs) | [3_1_ForeignKeysAndNavigation.md](Docs/04_Relations/3_1_ForeignKeysAndNavigation.md) |
| 3.10 | Налаштування зовнішнього ключа | [Lesson_3_10_ForeignKeyConfig.cs](src/04_Relations/Lesson_3_10_ForeignKeyConfig.cs) | [3_10_ForeignKeyConfig.md](Docs/04_Relations/3_10_ForeignKeyConfig.md) |
| 3.2 | Каскадне видалення | [Lesson_3_2_CascadeDelete.cs](src/04_Relations/Lesson_3_2_CascadeDelete.cs) | [3_2_CascadeDelete.md](Docs/04_Relations/3_2_CascadeDelete.md) |
| 3.3 | Завантаження пов'язаних даних (`Include`) | [Lesson_3_3_EagerLoadingInclude.cs](src/04_Relations/Lesson_3_3_EagerLoadingInclude.cs) | [3_3_EagerLoadingInclude.md](Docs/04_Relations/3_3_EagerLoadingInclude.md) |
| 3.8 | Явне завантаження (Explicit loading) | [Lesson_3_8_ExplicitLoading.cs](src/04_Relations/Lesson_3_8_ExplicitLoading.cs) | [3_8_ExplicitLoading.md](Docs/04_Relations/3_8_ExplicitLoading.md) |
| 3.9 | Ліниве завантаження (Lazy loading) | [Lesson_3_9_LazyLoading.cs](src/04_Relations/Lesson_3_9_LazyLoading.cs) | [3_9_LazyLoading.md](Docs/04_Relations/3_9_LazyLoading.md) |
| 3.4 | Відношення один до одного | [Lesson_3_4_OneToOne.cs](src/04_Relations/Lesson_3_4_OneToOne.cs) | [3_4_OneToOne.md](Docs/04_Relations/3_4_OneToOne.md) |
| 3.5 | Відношення один до багатьох | [Lesson_3_5_OneToMany.cs](src/04_Relations/Lesson_3_5_OneToMany.cs) | [3_5_OneToMany.md](Docs/04_Relations/3_5_OneToMany.md) |
| 3.6 | Відношення багато до багатьох | [Lesson_3_6_ManyToMany.cs](src/04_Relations/Lesson_3_6_ManyToMany.cs) | [3_6_ManyToMany.md](Docs/04_Relations/3_6_ManyToMany.md) |
| 3.7 | Власні типи (Owned Types) | [Lesson_3_7_OwnedTypes.cs](src/04_Relations/Lesson_3_7_OwnedTypes.cs) | [3_7_OwnedTypes.md](Docs/04_Relations/3_7_OwnedTypes.md) |
| 3.12 | Комплексні типи (Complex Types) | [Lesson_3_12_ComplexTypes.cs](src/04_Relations/Lesson_3_12_ComplexTypes.cs) | [3_12_ComplexTypes.md](Docs/04_Relations/3_12_ComplexTypes.md) |
| 3.11 | Ієрархічні дані | [Lesson_3_11_HierarchicalData.cs](src/04_Relations/Lesson_3_11_HierarchicalData.cs) | [3_11_HierarchicalData.md](Docs/04_Relations/3_11_HierarchicalData.md) |

### Глава 5. Спадкування (`05_Inheritance`)
| ID | Тема | Код | Конспект |
|---|---|---|---|
| 4.1 | Підхід TPH — Table Per Hierarchy | [Lesson_4_1_TphInheritance.cs](src/05_Inheritance/Lesson_4_1_TphInheritance.cs) | [4_1_TphInheritance.md](Docs/05_Inheritance/4_1_TphInheritance.md) |
| 4.2 | Підхід TPT — Table Per Type | [Lesson_4_2_TptInheritance.cs](src/05_Inheritance/Lesson_4_2_TptInheritance.cs) | [4_2_TptInheritance.md](Docs/05_Inheritance/4_2_TptInheritance.md) |
| 4.3 | Підхід TPC — Table Per Class | [Lesson_4_3_TpcInheritance.cs](src/05_Inheritance/Lesson_4_3_TpcInheritance.cs) | [4_3_TpcInheritance.md](Docs/05_Inheritance/4_3_TpcInheritance.md) |

### Глава 6. Запити та LINQ to Entities (`06_QueriesAndLinq`)
| ID | Тема | Код | Конспект |
|---|---|---|---|
| 5.1 | Вступ до LINQ to Entities | [Lesson_5_1_LinqIntro.cs](src/06_QueriesAndLinq/Lesson_5_1_LinqIntro.cs) | [5_1_LinqIntro.md](Docs/06_QueriesAndLinq/5_1_LinqIntro.md) |
| 5.2 | Вибірка та фільтрація | [Lesson_5_2_Filtering.cs](src/06_QueriesAndLinq/Lesson_5_2_Filtering.cs) | [5_2_Filtering.md](Docs/06_QueriesAndLinq/5_2_Filtering.md) |
| 5.10 | Сортування та проекція | [Lesson_5_10_SortingAndProjection.cs](src/06_QueriesAndLinq/Lesson_5_10_SortingAndProjection.cs) | [5_10_SortingAndProjection.md](Docs/06_QueriesAndLinq/5_10_SortingAndProjection.md) |
| 5.3 | З'єднання та групування таблиць | [Lesson_5_3_JoinAndGroupBy.cs](src/06_QueriesAndLinq/Lesson_5_3_JoinAndGroupBy.cs) | [5_3_JoinAndGroupBy.md](Docs/06_QueriesAndLinq/5_3_JoinAndGroupBy.md) |
| 5.4 | Операції з множинами | [Lesson_5_4_SetOperations.cs](src/06_QueriesAndLinq/Lesson_5_4_SetOperations.cs) | [5_4_SetOperations.md](Docs/06_QueriesAndLinq/5_4_SetOperations.md) |
| 5.5 | Агрегатні операції | [Lesson_5_5_Aggregates.cs](src/06_QueriesAndLinq/Lesson_5_5_Aggregates.cs) | [5_5_Aggregates.md](Docs/06_QueriesAndLinq/5_5_Aggregates.md) |
| 5.7 | Відстеження об'єктів та `AsNoTracking` | [Lesson_5_7_TrackingAndAsNoTracking.cs](src/06_QueriesAndLinq/Lesson_5_7_TrackingAndAsNoTracking.cs) | [5_7_TrackingAndAsNoTracking.md](Docs/06_QueriesAndLinq/5_7_TrackingAndAsNoTracking.md) |
| 5.8 | Виконання запитів | [Lesson_5_8_QueryExecution.cs](src/06_QueriesAndLinq/Lesson_5_8_QueryExecution.cs) | [5_8_QueryExecution.md](Docs/06_QueriesAndLinq/5_8_QueryExecution.md) |
| 5.6 | `IEnumerable` та `IQueryable` | [Lesson_5_6_IEnumerableVsIQueryable.cs](src/06_QueriesAndLinq/Lesson_5_6_IEnumerableVsIQueryable.cs) | [5_6_IEnumerableVsIQueryable.md](Docs/06_QueriesAndLinq/5_6_IEnumerableVsIQueryable.md) |
| 5.9 | Фільтри запитів рівня моделі | [Lesson_5_9_GlobalQueryFilters.cs](src/06_QueriesAndLinq/Lesson_5_9_GlobalQueryFilters.cs) | [5_9_GlobalQueryFilters.md](Docs/06_QueriesAndLinq/5_9_GlobalQueryFilters.md) |
| 5.11 | Масове оновлення та видалення | [Lesson_5_11_BulkExecuteUpdateDelete.cs](src/06_QueriesAndLinq/Lesson_5_11_BulkExecuteUpdateDelete.cs) | [5_11_BulkExecuteUpdateDelete.md](Docs/06_QueriesAndLinq/5_11_BulkExecuteUpdateDelete.md) |

### Глава 7. SQL в Entity Framework Core (`07_SqlAndStoredProcedures`)
| ID | Тема | Код | Конспект |
|---|---|---|---|
| 6.1 | Виконання SQL-запитів | [Lesson_6_1_RawSqlQueries.cs](src/07_SqlAndStoredProcedures/Lesson_6_1_RawSqlQueries.cs) | [6_1_RawSqlQueries.md](Docs/07_SqlAndStoredProcedures/6_1_RawSqlQueries.md) |
| 6.2 | Збережені функції | [Lesson_6_2_StoredFunctions.cs](src/07_SqlAndStoredProcedures/Lesson_6_2_StoredFunctions.cs) | [6_2_StoredFunctions.md](Docs/07_SqlAndStoredProcedures/6_2_StoredFunctions.md) |
| 6.3 | Збережені процедури | [Lesson_6_3_StoredProcedures.cs](src/07_SqlAndStoredProcedures/Lesson_6_3_StoredProcedures.cs) | [6_3_StoredProcedures.md](Docs/07_SqlAndStoredProcedures/6_3_StoredProcedures.md) |

### Глава 8. Додаткові теми (`08_AdvancedTopics`)
| ID | Тема | Код | Конспект |
|---|---|---|---|
| 2.11 | Паралелізм та оптимістичне блокування | [Lesson_2_11_Concurrency.cs](src/08_AdvancedTopics/Lesson_2_11_Concurrency.cs) | [2_11_Concurrency.md](Docs/08_AdvancedTopics/2_11_Concurrency.md) |
| 2.12 | Провайдери логування | [Lesson_2_12_LoggingProviders.cs](src/08_AdvancedTopics/Lesson_2_12_LoggingProviders.cs) | [2_12_LoggingProviders.md](Docs/08_AdvancedTopics/2_12_LoggingProviders.md) |
| 8.1 | Скомпільовані запити | [Lesson_8_1_CompiledQueries.cs](src/08_AdvancedTopics/Lesson_8_1_CompiledQueries.cs) | [8_1_CompiledQueries.md](Docs/08_AdvancedTopics/8_1_CompiledQueries.md) |
| 8.2 | Проєкція запитів на представлення (Views) | [Lesson_8_2_DatabaseViews.cs](src/08_AdvancedTopics/Lesson_8_2_DatabaseViews.cs) | [8_2_DatabaseViews.md](Docs/08_AdvancedTopics/8_2_DatabaseViews.md) |
| 8.3 | Зберігання історії змін | [Lesson_8_3_ChangeHistory.cs](src/08_AdvancedTopics/Lesson_8_3_ChangeHistory.cs) | [8_3_ChangeHistory.md](Docs/08_AdvancedTopics/8_3_ChangeHistory.md) |
