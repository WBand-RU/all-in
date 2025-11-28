# WBand - Платформа для музыкантов

Веб-приложение для музыкантов с функциями управления группами, редактирования песен, создания плейлистов и многим другим.

## Возможности

- 🎵 **Управление группами** - создание и приглашение участников
- 📝 **Редактор песен** - текст, аккорды, структура
- 🎼 **Плейлисты** - сетлисты для выступлений  
- 📁 **Файловое хранилище** - загрузка аудио и документов
- 🎚️ **Микшер** - работа с аудиодорожками
- 📚 **Wiki** - база знаний группы
- 🔐 **Авторизация** - JWT с ролевой моделью

## Технологии

- **Frontend**: Next.js 15, React 19, TypeScript, Tailwind CSS
- **Backend**: Next.js API Routes, MongoDB, Mongoose
- **Storage**: MinIO S3-совместимое хранилище
- **Auth**: JWT токены, bcrypt
- **State**: Zustand
- **Forms**: React Hook Form + Zod

## Установка и запуск

### Предварительные требования

- Node.js 18+
- pnpm
- Docker (для MongoDB и MinIO)

### 1. Установка зависимостей

```bash
pnpm install
```

### 2. Запуск баз данных

```bash
# В корне проекта
docker-compose up -d mongodb minio
```

### 3. Настройка окружения

Файл `.env.local` уже настроен со следующими переменными:

```env
MONGODB_URI=mongodb://wband:wband123@localhost:27017/wband?authSource=admin
JWT_SECRET=THIS_IS_A_SUPER_SECRET_KEY_FOR_JWT_TOKEN_GENERATION_AND_VALIDATION_CHANGE_IT_LATER
NEXT_PUBLIC_API_URL=http://localhost:3000
```

### 4. Запуск разработки

```bash
pnpm dev
```

Приложение будет доступно по адресу [http://localhost:3000](http://localhost:3000)

## Структура проекта (FSD)

```
src/
├── shared/           # Общие утилиты и компоненты
│   ├── lib/         # Утилиты (auth, mongodb, utils)
│   ├── ui/          # UI компоненты (button, input)
│   ├── store/       # Глобальные store (Zustand)
│   └── types/       # Типы TypeScript
├── entities/        # Бизнес-сущности
│   ├── user/        # Пользователь
│   ├── band/        # Группа
│   ├── song/        # Песня
│   └── playlist/    # Плейлист
├── features/        # Функциональность
│   └── auth/        # Авторизация (login/register)
├── widgets/         # Сложные компоненты
└── pages/           # Страницы приложения
```

## API Endpoints

### Авторизация
- `POST /api/auth/register` - Регистрация
- `POST /api/auth/login` - Вход

### Группы
- `GET /api/bands` - Список групп пользователя
- `POST /api/bands` - Создание группы
- `POST /api/bands/[id]/members` - Приглашение участника

## База данных

### MongoDB Collections:
- `users` - Пользователи
- `bands` - Группы с участниками и ролями
- `songs` - Песни с секциями и аккордами
- `playlists` - Плейлисты для выступлений

### MinIO Buckets:
- Аудиофайлы стемов
- Документы и изображения
- Экспортированные миксы

## Разработка

### Архитектура
Используется Feature-Sliced Design (FSD) для организации кода.

### Принципы SOLID
- Маленькие компоненты и функции
- Расширяемая архитектура
- Разделение ответственности

### Технические решения
- Mongoose для работы с MongoDB
- Minimal API подход
- JWT авторизация
- TypeScript строгая типизация
