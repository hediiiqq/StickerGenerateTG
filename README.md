# Sticergen

Sticergen - Telegram-бот на .NET для создания статичных стикерпаков из фотографий. Проект находится на стадии рабочего MVP: бот принимает фото, готовит изображение под требования Telegram, создает новый sticker set, сохраняет готовый пак в PostgreSQL и умеет добавлять новые стикеры в уже созданный пак.

AI-обработка уже подключена как экспериментальная ветка: можно выбрать provider/model, отправить фото с промтом и получить стилизованный стикер через Stability AI или Cloudflare Workers AI.

## Текущий статус

Работает:

- long polling для Telegram updates;
- обработка обычных сообщений, подписей к фото, ответов на фото и callback-кнопок;
- команды `/start`, `/help`, `/newpack`, `/addsticker`, `/mypacks`, `/aistatus`, `/aimodels`, `/aimodel`;
- создание черновика нового пака в PostgreSQL;
- скачивание исходного фото в `storage/originals`;
- подготовка финального `512x512` WebP-файла с прозрачным фоном и лимитом до `512 KB`;
- PNG-preview для отправки пользователю;
- создание Telegram sticker set через Bot API;
- сохранение созданного пака в таблицу `StickerPacks`;
- добавление нового стикера в существующий пак пользователя;
- переключение активной AI-модели командой или inline-кнопкой;
- миграции EF Core для черновиков, стикеров, готовых паков и AI-настроек.

Еще требует доработки:

- `/start`, unknown-команды и часть ответов остаются техническими;
- нет полноценного подтверждения через inline-кнопки перед созданием или добавлением стикера;
- нет тестов и production-инфраструктуры;
- `.gitignore` пока отсутствует, поэтому локальные `bin/`, `obj/`, `DB/`, `storage/` и graphify-артефакты нужно не коммитить вручную;
- ошибки Telegram API и AI API пока не всегда превращаются в понятные пользовательские сообщения.

## Технологии

- .NET 10 / C#
- Telegram.Bot
- Microsoft.Extensions.Hosting
- Entity Framework Core
- PostgreSQL / Npgsql
- SkiaSharp
- Stability AI image-to-image
- Cloudflare Workers AI

## Структура

```text
sticergen.sln
README.md
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
    generated/
    final/
graphify-out/
sticergen/graphify-out/
```

Ключевые файлы:

- `sticergen/src/Program.cs` - host, конфигурация, DI, Telegram-клиент, EF Core и сервисы.
- `sticergen/src/Infrastructure/TelegramBotHostedService.cs` - запуск long polling для message и callback query updates.
- `sticergen/src/Bot/TelegramUpdateHandler.cs` - извлечение команды, фото и callback-обработки.
- `sticergen/src/Bot/Commands/CommandHandler.cs` - основной сценарный слой команд бота.
- `sticergen/src/Bot/Commands/ArgumentsParser.cs` - разбор аргументов `/newpack` и `/addsticker`.
- `sticergen/src/Services/DraftService.cs` - черновики создания и добавления стикера.
- `sticergen/src/Services/StickerPackService.cs` - создание sticker set, добавление стикера и список паков.
- `sticergen/src/Services/ImageGenerationService.cs` - выбор raw/ai-ветки подготовки изображения.
- `sticergen/src/Services/ImageProcessingService.cs` - resize, прозрачный фон, WebP и PNG-preview.
- `sticergen/src/Services/StabilityImageProvider.cs` - Stability AI image-to-image.
- `sticergen/src/Services/CloudflareImageProvider.cs` - Cloudflare Workers AI.
- `sticergen/src/Services/ImageGenerationSettingsService.cs` - активная AI-модель в БД.

Архитектурные graphify-снимки лежат в `graphify-out/graph.html` и `sticergen/graphify-out/graph.html`. Более свежий снимок в корневом `graphify-out/` показывает, что основная точка связности сейчас - `CommandHandler`: через него проходят команды, зависимости, создание паков и callback-переключение AI-модели.

## Команды бота

```text
/start
/help
/mypacks
/newpack static raw Название пака
/newpack static ai Название пака | описание стилизации
/addsticker pack_name raw
/aistatus
/aimodels
/aimodel stability sd3.5-medium
/aimodel cloudflare @cf/runwayml/stable-diffusion-v1-5-img2img
```

Фото можно отправить с командой в подписи или написать команду ответом на сообщение с фото.

### `/newpack`

Создает черновик, сохраняет фото, готовит финальный файл и вызывает `CreateNewStickerSet`.

Примеры:

```text
/newpack static raw Мой пак
/newpack static ai Cyber cats | neon anime sticker style
```

Поддерживаемые стили:

- `raw` - resize до `512x512`, прозрачное поле, WebP для Telegram;
- `ai` - прогоняет фото через активный AI provider/model, затем готовит Telegram WebP.

### `/addsticker`

Ищет существующий пак пользователя по `pack_name`, создает draft в режиме `addsticker`, готовит файл и вызывает `AddStickerToSet`.

```text
/addsticker my_pack_123_by_bot raw
```

Сейчас команда поддерживает raw-пайплайн; AI-стилизация для добавления стикера не оформлена отдельным пользовательским сценарием.

### AI-команды

```text
/aistatus
/aimodels
/aimodel stability sd3.5-medium
/aimodel stability sd3.5-large
/aimodel stability sd3.5-large-turbo
/aimodel cloudflare @cf/runwayml/stable-diffusion-v1-5-img2img
/aimodel cloudflare @cf/lykon/dreamshaper-8-lcm
```

`/aimodels` отправляет inline-кнопки. Нажатие сохраняет активную модель в таблицу `ImageGenerationSettings`.

## Данные и файлы

PostgreSQL-модели:

- `Draft` - черновик операции `newpack` или `addsticker`;
- `DraftSticker` - фото и подготовленные файлы внутри черновика;
- `StickerPack` - созданный Telegram-пак пользователя;
- `ImageGenerationSetting` - активный AI provider/model.

Файлы:

- `storage/originals/{draftId}.png` - исходное фото из Telegram;
- `storage/generated/generated-{guid}.png` - результат AI-генерации;
- `storage/final/{draftId}.webp` - финальный стикер для Telegram;
- `storage/final/{draftId}-preview.png` - preview для отправки в чат.

## Установка

1. Установите .NET SDK 10.

2. Поднимите PostgreSQL и создайте базу, например `sticergen`.

3. Перейдите в папку проекта:

```bash
cd sticergen
```

4. Восстановите зависимости:

```bash
dotnet restore
```

5. Настройте секреты. Не храните токены и пароли в git.

```bash
dotnet user-secrets set "Telegram:BotToken" "YOUR_TELEGRAM_BOT_TOKEN"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=sticergen;Username=postgres;Password=postgres"
```

Для AI-режима:

```bash
dotnet user-secrets set "ImageGeneration:Provider" "stability"
dotnet user-secrets set "ImageGeneration:Model" "sd3.5-medium"
dotnet user-secrets set "ImageGeneration:StabilityApiKey" "YOUR_STABILITY_API_KEY"
```

Или для Cloudflare:

```bash
dotnet user-secrets set "ImageGeneration:Provider" "cloudflare"
dotnet user-secrets set "ImageGeneration:Model" "@cf/runwayml/stable-diffusion-v1-5-img2img"
dotnet user-secrets set "ImageGeneration:CloudflareApiKey" "YOUR_CLOUDFLARE_API_KEY"
dotnet user-secrets set "ImageGeneration:CloudflareAccountId" "YOUR_CLOUDFLARE_ACCOUNT_ID"
```

6. Установите EF CLI, если он еще не установлен:

```bash
dotnet tool install --global dotnet-ef
```

7. Примените миграции:

```bash
dotnet ef database update
```

## Запуск

Из папки `sticergen`:

```bash
dotnet run
```

После успешного запуска в консоли появится:

```text
Подключён бот: @bot_username
```

## Проверка

Быстрая проверка сборки из корня репозитория:

```bash
dotnet build sticergen.sln
```

Минимальная ручная проверка:

1. Отправить боту `/help`.
2. Отправить фото с подписью `/newpack static raw Мой пак`.
3. Проверить, что бот отправил preview и ссылку на sticker set.
4. Отправить `/mypacks` и убедиться, что созданный пак появился в списке.
5. Отправить новое фото с подписью `/addsticker pack_name raw`.
6. Проверить, что стикер добавился в Telegram-пак.

Проверка AI-ветки:

1. Настроить ключ Stability или Cloudflare.
2. Выполнить `/aistatus`.
3. При необходимости переключить модель через `/aimodels`.
4. Отправить фото с подписью `/newpack static ai AI пак | cartoon superhero sticker`.

## Roadmap

Ближайшие задачи:

- добавить `.gitignore` и убрать локальные артефакты из рабочей области;
- заменить технические ответы на нормальные пользовательские сообщения;
- вернуть outline-режим, когда будет понятен дешевый provider удаления фона;
- добавить подтверждение создания/добавления через inline-кнопки;
- убрать дублирование сообщений в `/addsticker`;
- добавить понятную обработку ошибок Telegram API, SkiaSharp и AI API;
- покрыть парсер команд, генератор имен и основные сервисы тестами;
- подготовить Dockerfile и `docker-compose` для PostgreSQL;
- добавить логирование через `ILogger`.

Дальше:

- collage preview для нескольких стикеров;
- ручной выбор emoji;
- AI-анализ фото для emoji;
- video/WebM stickers;
- админ-панель;
- статистика использования.
