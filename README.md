# Sticergen

## Русский

Sticergen - это Telegram-бот на .NET для подготовки черновиков стикерпаков из фотографий. Сейчас проект умеет принимать команды из текста, подписи к фото или ответа на фото, сохранять черновики в PostgreSQL, скачивать оригинальное изображение из Telegram и делать PNG-превью размером 512 x 512 пикселей.

Проект находится в активной разработке: базовый workflow создания черновика уже работает, но публикация готового стикерпака в Telegram и часть пользовательских сценариев еще не реализованы.

### Возможности

- Обработка Telegram-сообщений через long polling.
- Поддержка команд `/start`, `/help`, `/mypacks`, `/newpack`, `/addsticker`.
- Создание черновика стикерпака по команде `/newpack`.
- Привязка первого фото к черновику.
- Сохранение исходников в `storage/originals`.
- Генерация итогового PNG-превью в `storage/final`.
- Хранение черновиков и стикеров в PostgreSQL через Entity Framework Core.
- Миграции базы данных для таблиц `Drafts` и `DraftStickers`.

### Технологии

- .NET 10
- C#
- Telegram.Bot
- Entity Framework Core
- PostgreSQL / Npgsql
- SkiaSharp
- Microsoft.Extensions.Hosting

### Структура проекта

```text
sticergen.sln
sticergen/
  sticergen.csproj
  appsettings.json
  src/
    Program.cs
    Bot/
      TelegramUpdateHandler.cs
      Commands/
      Models/
    Configuration/
    Data/
      AppDbContext.cs
      Models/
      Migrations/
    Infrastructure/
    Services/
  storage/
    originals/
    final/
```

Ключевые файлы:

- `src/Program.cs` - настройка host, DI, Telegram-клиента, EF Core и сервисов.
- `src/Infrastructure/TelegramBotHostedService.cs` - запуск long polling.
- `src/Bot/TelegramUpdateHandler.cs` - получение текста команды, фото и создание контекста.
- `src/Bot/Commands/CommandHandler.cs` - выполнение команд бота.
- `src/Bot/Commands/CommandParser.cs` - определение типа команды.
- `src/Bot/Commands/ArgumentsParser.cs` - разбор аргументов `/newpack` и `/addsticker`.
- `src/Services/DraftService.cs` - работа с черновиками в базе.
- `src/Services/FileStorageService.cs` - скачивание оригинальных фото из Telegram.
- `src/Services/ImageProcessingService.cs` - подготовка PNG 512 x 512 через SkiaSharp.

### Команды бота

```text
/start
/help
/mypacks
/newpack static raw Название пака
/newpack static outline Название пака
/addsticker pack_name raw
```

`/newpack` требует фото. Фото можно отправить с подписью-командой или написать команду ответом на сообщение с фото.

Текущий статус команд:

- `/start` - отвечает техническим сообщением `start`.
- `/help` - показывает список команд.
- `/mypacks` - показывает список черновиков пользователя.
- `/newpack` - создает черновик, сохраняет фото, генерирует preview PNG и отправляет его в чат.
- `/addsticker` - пока только разбирает аргументы и отправляет диагностический ответ.
- неизвестная команда - отвечает техническим сообщением `unknown`.

### Установка

1. Установите .NET SDK 10.

2. Поднимите PostgreSQL и создайте базу данных, например `sticergen`.

3. Перейдите в папку проекта:

```bash
cd sticergen
```

4. Восстановите зависимости:

```bash
dotnet restore
```

5. Настройте секреты. Не храните токен бота и пароль от базы в git.

```bash
dotnet user-secrets set "Telegram:BotToken" "YOUR_TELEGRAM_BOT_TOKEN"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=sticergen;Username=postgres;Password=postgres"
```

Если `dotnet ef` не установлен:

```bash
dotnet tool install --global dotnet-ef
```

6. Примените миграции:

```bash
dotnet ef database update
```

### Запуск

Из папки `sticergen`:

```bash
dotnet run
```

После успешного запуска в консоли появится:

```text
Подключён бот: @bot_username
```

### Проверка

```bash
dotnet build
```

Минимальная ручная проверка:

1. Напишите боту `/help`.
2. Отправьте фото с подписью `/newpack static raw Мой пак`.
3. Проверьте, что бот вернул preview.
4. Проверьте файлы `storage/originals/{draftId}.png` и `storage/final/{draftId}.png`.
5. Напишите `/mypacks` и убедитесь, что черновик появился в списке.

### Telegram Sticker Pack Bot - Roadmap

#### Phase 1 - Infrastructure

- [x] Создать solution.
- [x] Создать GitHub репозиторий.
- [ ] Настроить `.gitignore`.
- [x] Подключить `Telegram.Bot`.
- [x] Подключить PostgreSQL.
- [x] Подключить Entity Framework Core.
- [x] Создать `appsettings.json`.
- [x] Настроить Dependency Injection.
- [x] Настроить Hosted Service для Telegram Bot.
- [x] Проверить сборку приложения.
- [ ] Проверить запуск приложения с реальным токеном и базой.

#### Phase 2 - Database

- [x] Создать `AppDbContext`.
- [ ] Создать модель `StickerPack`.
- [x] Создать модель `Draft`.
- [x] Создать модель `DraftSticker`.
- [ ] Создать перечисления `Enums`.
- [x] Настроить связь `Draft` -> `DraftSticker`.
- [ ] Настроить связи для готовых стикерпаков.
- [x] Создать первую миграцию.
- [x] Создать миграцию для `DraftSticker`.
- [ ] Применить миграции в чистой базе.
- [ ] Проверить создание таблиц в PostgreSQL.

#### Phase 3 - Telegram Updates

- [x] Реализовать получение `Update`.
- [x] Реализовать обработку `Message`.
- [ ] Реализовать обработку `CallbackQuery`.
- [x] Реализовать базовую обработку ошибок Telegram API.
- [ ] Добавить логирование через `ILogger`.

#### Phase 4 - Commands

- [x] Команда `/start`.
- [x] Команда `/help`.
- [x] Команда `/newpack`.
- [x] Команда `/addsticker`.
- [x] Команда `/mypacks`.
- [ ] Команда `/about`.
- [ ] Валидация аргументов команд.
- [x] Обработка неизвестных команд.
- [ ] Заменить технические ответы на нормальные пользовательские сообщения.
- [ ] Исправить разбор команд с лишними пробелами.

#### Phase 5 - Draft System

- [x] Создание черновика.
- [x] Получение черновиков пользователя.
- [ ] Получение одного черновика по id или имени.
- [ ] Обновление статуса черновика.
- [ ] Отмена черновика.
- [ ] Проверка владельца черновика.
- [ ] Очистка старых черновиков.

#### Phase 6 - File Storage

- [x] Скачивание фото из Telegram.
- [x] Сохранение оригинала.
- [x] Сохранение preview.
- [ ] Сохранение готового стикера как отдельной сущности.
- [x] Генерация уникальных имен файлов на основе `draftId`.
- [ ] Очистка временных файлов.

#### Phase 7 - Image Processing

Raw Style:

- [x] Resize до 512 x 512.
- [x] Сохранение пропорций.
- [x] Добавление прозрачного фона.
- [x] Экспорт изображения в PNG.
- [ ] Вынести размер 512 в константу.

Outline Style:

- [ ] Создать реализацию outline.
- [ ] Добавить белую обводку.
- [ ] Проверить качество результата.

#### Phase 8 - Emoji System

- [ ] Создать `EmojiService`.
- [ ] Автоматически назначать emoji.
- [x] Добавить поле `Emoji` в модель `DraftSticker`.
- [ ] Показывать emoji в preview.
- [ ] Добавить возможность расширения логики.

#### Phase 9 - Preview Generation

- [ ] Создать `PreviewService`.
- [x] Генерация preview для одного изображения.
- [ ] Генерация коллажа для нескольких изображений.
- [ ] Добавление информации об emoji.
- [x] Сохранение preview.
- [x] Отправка preview пользователю.

#### Phase 10 - Inline Buttons

- [ ] Кнопка "Создать".
- [ ] Кнопка "Перегенерировать".
- [ ] Кнопка "Отменить".
- [ ] Проверка владельца черновика.
- [ ] Защита от повторного нажатия.

#### Phase 11 - Sticker Pack Creation

- [ ] Создание нового стикерпака.
- [ ] Генерация уникального имени.
- [ ] Создание Telegram Sticker Set.
- [ ] Добавление первого стикера.
- [ ] Сохранение информации о готовом паке в БД.
- [ ] Отправка ссылки пользователю.

#### Phase 12 - Add Sticker

- [ ] Поиск существующего пака.
- [ ] Проверка владельца.
- [ ] Скачивание фото для нового стикера.
- [ ] Создание preview.
- [ ] Подтверждение через кнопки.
- [ ] Добавление стикера в пак.

#### Phase 13 - My Packs

- [x] Получение списка черновиков пользователя.
- [ ] Получение списка готовых паков пользователя.
- [ ] Вывод ссылок на паки.
- [ ] Красивое форматирование ответа.
- [ ] Обработка пустого списка.

#### Phase 14 - Error Handling

- [x] Фото не прикреплено.
- [x] Неверная или неизвестная команда.
- [ ] Неверный формат аргументов.
- [ ] Неизвестный стиль.
- [ ] Пак не найден.
- [ ] Чужой пак.
- [ ] Ошибка Telegram API с понятным ответом пользователю.
- [ ] Ошибка обработки изображения.
- [ ] Черновик не найден.
- [ ] Истекший черновик.

#### Phase 15 - Production Ready

- [x] Структурировать сервисы.
- [ ] Убрать дублирование кода.
- [x] Добавить конфигурацию через Options Pattern.
- [ ] Добавить логирование через `ILogger`.
- [ ] Добавить health checks.
- [ ] Подготовить Dockerfile.
- [ ] Подготовить `docker-compose`.
- [x] Подготовить README.
- [ ] Добавить тесты.

#### Future Features

- [ ] Video stickers.
- [ ] AI-анализ фото для выбора emoji.
- [ ] Ручное изменение emoji.
- [ ] Дополнительные стили.
- [ ] Поддержка WEBM stickers.
- [ ] Платные подписки.
- [ ] Web-панель администратора.
- [ ] Статистика использования.

### Известные ограничения

- `/addsticker` пока не добавляет файл в черновик.
- Неполные команды могут приводить к ошибкам разбора аргументов.
- Лишние пробелы в командах обрабатываются не во всех случаях.
- Бот пока не создает настоящий Telegram-стикерпак, а только черновик и preview.
- Пользовательские ответы местами технические и требуют полировки.
- В репозитории пока нет `.gitignore`, поэтому локальные артефакты нужно контролировать вручную.

## English

Sticergen is a .NET Telegram bot for preparing sticker pack drafts from photos. The current version can read commands from plain text, photo captions, or replies to photos, store drafts in PostgreSQL, download original images from Telegram, and generate 512 x 512 PNG previews.

The project is still in active development. The base draft creation workflow works, but publishing a real Telegram sticker pack and several user-facing flows are not implemented yet.

### Features

- Telegram message handling through long polling.
- Commands: `/start`, `/help`, `/mypacks`, `/newpack`, `/addsticker`.
- Draft sticker pack creation through `/newpack`.
- First photo attachment for a draft.
- Original image storage in `storage/originals`.
- Final PNG preview generation in `storage/final`.
- Draft and sticker persistence in PostgreSQL with Entity Framework Core.
- Database migrations for `Drafts` and `DraftStickers`.

### Tech Stack

- .NET 10
- C#
- Telegram.Bot
- Entity Framework Core
- PostgreSQL / Npgsql
- SkiaSharp
- Microsoft.Extensions.Hosting

### Project Structure

```text
sticergen.sln
sticergen/
  sticergen.csproj
  appsettings.json
  src/
    Program.cs
    Bot/
      TelegramUpdateHandler.cs
      Commands/
      Models/
    Configuration/
    Data/
      AppDbContext.cs
      Models/
      Migrations/
    Infrastructure/
    Services/
  storage/
    originals/
    final/
```

Important files:

- `src/Program.cs` - host, dependency injection, Telegram client, EF Core, and services.
- `src/Infrastructure/TelegramBotHostedService.cs` - long polling startup.
- `src/Bot/TelegramUpdateHandler.cs` - extracts command text, photos, and command context.
- `src/Bot/Commands/CommandHandler.cs` - executes bot commands.
- `src/Bot/Commands/CommandParser.cs` - detects command type.
- `src/Bot/Commands/ArgumentsParser.cs` - parses `/newpack` and `/addsticker` arguments.
- `src/Services/DraftService.cs` - draft persistence.
- `src/Services/FileStorageService.cs` - downloads original photos from Telegram.
- `src/Services/ImageProcessingService.cs` - creates 512 x 512 PNG previews with SkiaSharp.

### Bot Commands

```text
/start
/help
/mypacks
/newpack static raw Pack title
/newpack static outline Pack title
/addsticker pack_name raw
```

`/newpack` requires a photo. You can send a photo with the command as a caption or reply to a photo with the command.

Current command status:

- `/start` - returns the technical message `start`.
- `/help` - shows the command list.
- `/mypacks` - shows the user's draft list.
- `/newpack` - creates a draft, stores the photo, generates a PNG preview, and sends it back to the chat.
- `/addsticker` - currently only parses arguments and sends a diagnostic response.
- unknown command - returns the technical message `unknown`.

### Installation

1. Install .NET SDK 10.

2. Start PostgreSQL and create a database, for example `sticergen`.

3. Go to the project directory:

```bash
cd sticergen
```

4. Restore dependencies:

```bash
dotnet restore
```

5. Configure secrets. Do not store the bot token or database password in git.

```bash
dotnet user-secrets set "Telegram:BotToken" "YOUR_TELEGRAM_BOT_TOKEN"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=sticergen;Username=postgres;Password=postgres"
```

If `dotnet ef` is not installed:

```bash
dotnet tool install --global dotnet-ef
```

6. Apply migrations:

```bash
dotnet ef database update
```

### Running

From the `sticergen` directory:

```bash
dotnet run
```

On successful startup the console prints:

```text
Подключён бот: @bot_username
```

### Verification

```bash
dotnet build
```

Minimal manual check:

1. Send `/help` to the bot.
2. Send a photo with the caption `/newpack static raw My pack`.
3. Check that the bot returns a preview.
4. Check `storage/originals/{draftId}.png` and `storage/final/{draftId}.png`.
5. Send `/mypacks` and confirm that the draft appears in the list.

### Telegram Sticker Pack Bot - Roadmap

#### Phase 1 - Infrastructure

- [x] Create the solution.
- [x] Create the GitHub repository.
- [ ] Configure `.gitignore`.
- [x] Add `Telegram.Bot`.
- [x] Add PostgreSQL.
- [x] Add Entity Framework Core.
- [x] Create `appsettings.json`.
- [x] Configure Dependency Injection.
- [x] Configure the Telegram Bot Hosted Service.
- [x] Verify that the application builds.
- [ ] Verify startup with a real bot token and database.

#### Phase 2 - Database

- [x] Create `AppDbContext`.
- [ ] Create the `StickerPack` model.
- [x] Create the `Draft` model.
- [x] Create the `DraftSticker` model.
- [ ] Create `Enums`.
- [x] Configure the `Draft` -> `DraftSticker` relationship.
- [ ] Configure relationships for published sticker packs.
- [x] Create the initial migration.
- [x] Create the `DraftSticker` migration.
- [ ] Apply migrations on a clean database.
- [ ] Verify table creation in PostgreSQL.

#### Phase 3 - Telegram Updates

- [x] Implement `Update` receiving.
- [x] Implement `Message` handling.
- [ ] Implement `CallbackQuery` handling.
- [x] Implement basic Telegram API error handling.
- [ ] Add logging through `ILogger`.

#### Phase 4 - Commands

- [x] `/start` command.
- [x] `/help` command.
- [x] `/newpack` command.
- [x] `/addsticker` command.
- [x] `/mypacks` command.
- [ ] `/about` command.
- [ ] Command argument validation.
- [x] Unknown command handling.
- [ ] Replace technical replies with proper user-facing messages.
- [ ] Fix command parsing with extra spaces.

#### Phase 5 - Draft System

- [x] Draft creation.
- [x] User draft list retrieval.
- [ ] Single draft lookup by id or name.
- [ ] Draft status update.
- [ ] Draft cancellation.
- [ ] Draft owner check.
- [ ] Old draft cleanup.

#### Phase 6 - File Storage

- [x] Download photos from Telegram.
- [x] Save the original file.
- [x] Save the preview.
- [ ] Save the final sticker as a separate entity.
- [x] Generate unique filenames based on `draftId`.
- [ ] Clean temporary files.

#### Phase 7 - Image Processing

Raw Style:

- [x] Resize to 512 x 512.
- [x] Preserve aspect ratio.
- [x] Add transparent background.
- [x] Export image as PNG.
- [ ] Move the 512 size into a constant.

Outline Style:

- [ ] Implement outline style.
- [ ] Add white outline.
- [ ] Verify result quality.

#### Phase 8 - Emoji System

- [ ] Create `EmojiService`.
- [ ] Assign emoji automatically.
- [x] Add the `Emoji` field to `DraftSticker`.
- [ ] Show emoji in previews.
- [ ] Make emoji logic extensible.

#### Phase 9 - Preview Generation

- [ ] Create `PreviewService`.
- [x] Generate a preview for one image.
- [ ] Generate a collage for multiple images.
- [ ] Add emoji information.
- [x] Save preview.
- [x] Send preview to the user.

#### Phase 10 - Inline Buttons

- [ ] "Create" button.
- [ ] "Regenerate" button.
- [ ] "Cancel" button.
- [ ] Owner check.
- [ ] Protection from repeated clicks.

#### Phase 11 - Sticker Pack Creation

- [ ] Create a new sticker pack.
- [ ] Generate a unique pack name.
- [ ] Create a Telegram Sticker Set.
- [ ] Add the first sticker.
- [ ] Save published pack information in the database.
- [ ] Send the pack link to the user.

#### Phase 12 - Add Sticker

- [ ] Find an existing pack.
- [ ] Check ownership.
- [ ] Download the new sticker photo.
- [ ] Create a preview.
- [ ] Confirm through inline buttons.
- [ ] Add the sticker to the pack.

#### Phase 13 - My Packs

- [x] Retrieve the user's draft list.
- [ ] Retrieve the user's published packs.
- [ ] Show pack links.
- [ ] Format the response nicely.
- [ ] Handle an empty list.

#### Phase 14 - Error Handling

- [x] Missing photo.
- [x] Invalid or unknown command.
- [ ] Invalid argument format.
- [ ] Unknown style.
- [ ] Pack not found.
- [ ] Pack belongs to another user.
- [ ] Telegram API error with a clear user-facing response.
- [ ] Image processing error.
- [ ] Draft not found.
- [ ] Expired draft.

#### Phase 15 - Production Ready

- [x] Structure services.
- [ ] Remove duplicated code.
- [x] Add configuration through Options Pattern.
- [ ] Add logging through `ILogger`.
- [ ] Add health checks.
- [ ] Prepare Dockerfile.
- [ ] Prepare `docker-compose`.
- [x] Prepare README.
- [ ] Add tests.

#### Future Features

- [ ] Video stickers.
- [ ] AI photo analysis for emoji selection.
- [ ] Manual emoji editing.
- [ ] Additional styles.
- [ ] WEBM sticker support.
- [ ] Paid subscriptions.
- [ ] Web admin panel.
- [ ] Usage statistics.

### Known Limitations

- `/addsticker` does not add files to drafts yet.
- Incomplete commands can still break argument parsing.
- Commands with extra spaces are not handled consistently.
- The bot does not create a real Telegram sticker pack yet; it only creates a draft and preview.
- Some user-facing replies are still technical and need polishing.
- There is no `.gitignore` yet, so local artifacts must be controlled manually.
