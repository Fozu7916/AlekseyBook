import React, { useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { userService } from '../../services/userService';
import './Auth.css';

const Auth: React.FC = () => {
  const [searchParams] = useSearchParams();
  const [isRegister, setIsRegister] = useState(searchParams.get('register') === 'true');
  const [formData, setFormData] = useState({
    username: '',
    email: '',
    password: ''
  });
  const [error, setError] = useState<string>('');
  const [isLoading, setIsLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setIsLoading(true);

    try {
      let response;
      if (isRegister) {
        response = await userService.register(formData);
      } else {
        response = await userService.login({
          email: formData.email,
          password: formData.password
        });
      }
      
      // После успешного входа перезагружаем страницу
      window.location.href = '/';
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Произошла ошибка');
    } finally {
      setIsLoading(false);
    }
  };

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setFormData({
      ...formData,
      [e.target.name]: e.target.value
    });
  };

  return (
    <div className="auth-page">
      <div className="auth-container">
        <h2>{isRegister ? 'Регистрация' : 'Вход'}</h2>
        
        {error && <div className="error-message">{error}</div>}
        
        <form onSubmit={handleSubmit}>
          {isRegister && (
            <div className="form-group">
              <label htmlFor="username">Имя пользователя</label>
              <input
                type="text"
                id="username"
                name="username"
                value={formData.username}
                onChange={handleInputChange}
                required
                disabled={isLoading}
              />
            </div>
          )}
          
          <div className="form-group">
            <label htmlFor="email">Email</label>
            <input
              type="email"
              id="email"
              name="email"
              value={formData.email}
              onChange={handleInputChange}
              required
              disabled={isLoading}
            />
          </div>
          
          <div className="form-group">
            <label htmlFor="password">Пароль</label>
            <input
              type="password"
              id="password"
              name="password"
              value={formData.password}
              onChange={handleInputChange}
              required
              disabled={isLoading}
            />
          </div>
          
          <button type="submit" className="submit-button" disabled={isLoading}>
            {isLoading ? 'Загрузка...' : (isRegister ? 'Зарегистрироваться' : 'Войти')}
          </button>
        </form>
        
        <div className="auth-switch">
          {isRegister ? (
            <p>
              Уже есть аккаунт?{' '}
              <button onClick={() => setIsRegister(false)} disabled={isLoading}>
                Войти
              </button>
            </p>
          ) : (
            <p>
              Нет аккаунта?{' '}
              <button onClick={() => setIsRegister(true)} disabled={isLoading}>
                Зарегистрироваться
              </button>
            </p>
          )}
        </div>
      </div>
    </div>
  );
};

export default Auth; 