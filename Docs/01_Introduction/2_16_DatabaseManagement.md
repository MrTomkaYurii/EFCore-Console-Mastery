# 2.16. Управління базою даних

- **Глава:** 01. Вступ до Entity Framework Core
- **Оригінал на Metanit:** [https://metanit.com/sharp/efcore/2.16.php](https://metanit.com/sharp/efcore/2.16.php)
- **Код прикладу:** [`Lesson_2_16_DatabaseManagement.cs`](../../src/01_Introduction/Lesson_2_16_DatabaseManagement.cs)

## Мета уроку

Ознайомитися з властивістю `DbContext.Database` (`DatabaseFacade`) та її основними методами для програмного створення, видалення та перевірки доступності бази даних у синхронному й асинхронному режимах.

## Теоретичний матеріал

У класі `DbContext` визначено властивість `Database` типу `Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade`. Вона надає низькорівневий інтерфейс для керування самою базою даних.

### Основні методи `DatabaseFacade`

| Синхронний метод | Асинхронний метод | Призначення та повернене значення |
| --- | --- | --- |
| `EnsureCreated()` | `EnsureCreatedAsync()` | Створює БД та таблиці за моделлю, якщо БД або таблиць ще немає. Повертає `true`, якщо БД/схему створено, і `false`, якщо вона вже існувала |
| `EnsureDeleted()` | `EnsureDeletedAsync()` | Видаляє БД, якщо вона існує. Повертає `true`, якщо БД видалено, і `false`, якщо її не було |
| `CanConnect()` | `CanConnectAsync()` | Перевіряє, чи можна встановити з'єднання з БД. Повертає `bool` |

### Поведінка `EnsureCreated()`
1. Якщо БД відсутня — створює БД і всі таблиці згідно з моделлю контексту.
2. Якщо БД існує, але **порожня** (не містить таблиць) — створює таблиці.
3. Якщо БД існує і **вже містить таблиці** — не робить нічого (навіть якщо схема моделі змінилася!).

## Ключові фрагменти коду

Комбінація `EnsureDeleted()` + `EnsureCreated()` для чистого перезапуску в тестах/прототипах та перевірка `CanConnect()`:

```csharp
bool deleted = db.Database.EnsureDeleted();
bool created = db.Database.EnsureCreated();
bool canConnect = db.Database.CanConnect();

// Асинхронні еквіваленти
bool deletedAsync = await db.Database.EnsureDeletedAsync();
bool createdAsync = await db.Database.EnsureCreatedAsync();
bool canConnectAsync = await db.Database.CanConnectAsync();
```

## Особливості роботи в SQLite

- Для SQLite `EnsureDeleted()` фізично видаляє файл `.db` з диска.
- Для SQLite `CanConnect()` перевіряє можливість відкриття файлу бази даних; якщо файл ще не створений, SQLite-драйвер може поводитися інакше, ніж клієнт-серверна СУБД (у SQL Server при відсутності БД `CanConnect()` повертає `false`, а в SQLite перевірка залежить від параметрів рядка підключення та прав доступу до каталогу).
- `EnsureCreated()` зручний для SQLite у навчальних і тестових сценаріях, але **не оновлює** схему існуючого `.db`-файлу після додавання нових властивостей до моделей.

## Підсумок та підводні камені

- **Не змішуйте `EnsureCreated()` і міграції (`Migrate()`):** якщо БД створена через `EnsureCreated()`, у ній не буде таблиці `__EFMigrationsHistory`, і подальший виклик `Migrate()` завершиться помилкою.
- Ніколи не залишайте `EnsureDeleted()` у продакшн-коді або в конструкторі контексту поза тестами — це призведе до повної втрати даних.
- У вебзастосунках і сучасних сервісах віддавайте перевагу асинхронним методам (`EnsureCreatedAsync()`, `CanConnectAsync()`), щоб не блокувати потоки.
