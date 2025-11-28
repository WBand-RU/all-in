'use client'

import { useState } from 'react'
import { useAuthStore } from 'shared/store/auth-store'
import { LoginForm } from 'features/auth/ui/login-form'
import { RegisterForm } from 'features/auth/ui/register-form'
import { Button } from 'shared/ui/button'
import Link from "next/link"

export default function Home() {
  const { isAuthenticated, user, logout } = useAuthStore()
  const [showLogin, setShowLogin] = useState(true)

  const handleAuthSuccess = (data: { user: any; token: string }) => {
    useAuthStore.getState().login(data.user, data.token)
  }

  if (isAuthenticated && user) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100 p-8">
        <div className="max-w-4xl mx-auto">
          <header className="flex justify-between items-center mb-8">
            <h1 className="text-3xl font-bold text-gray-800">WBand</h1>
            <div className="flex items-center gap-4">
              <span className="text-gray-600">Привет, {user.firstName || user.username}!</span>
              <Button variant="outline" onClick={logout}>
                Выйти
              </Button>
            </div>
          </header>

          <main className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            <div className="bg-white rounded-lg p-6 shadow-md">
              <h2 className="text-xl font-semibold mb-4 text-gray-800">Мои группы</h2>
              <p className="text-gray-600 mb-4">
                Создавайте и управляйте своими музыкальными группами
              </p>
              <Link href="/bands" className="w-full">Управление группами</Link>
            </div>

            <div className="bg-white rounded-lg p-6 shadow-md">
              <h2 className="text-xl font-semibold mb-4 text-gray-800">Песни</h2>
              <p className="text-gray-600 mb-4">
                Редактируйте тексты, аккорды и структуру песен
              </p>
              <Button className="w-full">Редактор песен</Button>
            </div>

            <div className="bg-white rounded-lg p-6 shadow-md">
              <h2 className="text-xl font-semibold mb-4 text-gray-800">Плейлисты</h2>
              <p className="text-gray-600 mb-4">
                Создавайте сетлисты для выступлений и репетиций
              </p>
              <Button className="w-full">Создать плейлист</Button>
            </div>

            <div className="bg-white rounded-lg p-6 shadow-md">
              <h2 className="text-xl font-semibold mb-4 text-gray-800">Файлы</h2>
              <p className="text-gray-600 mb-4">
                Загружайте и управляйте аудиофайлами
              </p>
              <Button className="w-full">Загрузить файлы</Button>
            </div>

            <div className="bg-white rounded-lg p-6 shadow-md">
              <h2 className="text-xl font-semibold mb-4 text-gray-800">Микшер</h2>
              <p className="text-gray-600 mb-4">
                Смешивайте аудиодорожки и скачивайте результат
              </p>
              <Link href="/mixer" className="w-full">
                Открыть микшер
              </Link>
            </div>

            <div className="bg-white rounded-lg p-6 shadow-md">
              <h2 className="text-xl font-semibold mb-4 text-gray-800">Wiki</h2>
              <p className="text-gray-600 mb-4">
                База знаний для вашей группы
              </p>
              <Button className="w-full">Открыть Wiki</Button>
            </div>
          </main>
        </div>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100 flex items-center justify-center p-8">
      <div className="bg-white rounded-lg shadow-lg p-8 w-full max-w-md">
        <div className="text-center mb-8">
          <h1 className="text-3xl font-bold text-gray-800 mb-2">WBand</h1>
          <p className="text-gray-600">Платформа для музыкантов</p>
        </div>

        <div className="mb-6">
          <div className="flex rounded-lg bg-gray-100 p-1">
            <button
              className={`flex-1 py-2 px-4 rounded-md text-sm font-medium transition ${
                showLogin 
                  ? 'bg-white text-gray-900 shadow-sm' 
                  : 'text-gray-600 hover:text-gray-900'
              }`}
              onClick={() => setShowLogin(true)}
            >
              Вход
            </button>
            <button
              className={`flex-1 py-2 px-4 rounded-md text-sm font-medium transition ${
                !showLogin 
                  ? 'bg-white text-gray-900 shadow-sm' 
                  : 'text-gray-600 hover:text-gray-900'
              }`}
              onClick={() => setShowLogin(false)}
            >
              Регистрация
            </button>
          </div>
        </div>

        {showLogin ? (
          <LoginForm onSuccess={handleAuthSuccess} />
        ) : (
          <RegisterForm onSuccess={handleAuthSuccess} />
        )}
      </div>
    </div>
  )
}
