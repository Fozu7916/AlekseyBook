import { render, screen, fireEvent, act } from '@testing-library/react';
import Auth from '../../pages/Auth/Auth';
import { userService } from '../../services/userService';

// Мокируем сервис
jest.mock('../../services/userService', () => ({
  userService: {
    login: jest.fn(),
    register: jest.fn()
  }
}));

jest.mock('react-router-dom');

describe('Auth Component', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  test('renders login form by default', () => {
    render(<Auth />);
    
    expect(screen.getByLabelText(/email/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/пароль/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /войти/i })).toBeInTheDocument();
  });

  test('switches to registration form', async () => {
    render(<Auth />);
    
    // Находим кнопку переключения на форму регистрации
    const switchButton = screen.getByRole('button', { name: /зарегистрироваться/i });
    
    // Используем act для обёртки действия, которое вызывает изменение состояния
    await act(async () => {
      fireEvent.click(switchButton);
    });

    // Проверяем, что форма переключилась в режим регистрации
    expect(screen.getByText(/регистрация/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /зарегистрироваться/i })).toBeInTheDocument();
  });

  test('handles successful login', async () => {
    const mockLoginResponse = { token: 'test-token', user: { id: '1', username: 'test', email: 'test@example.com' } };
    const mockLogin = jest.fn().mockResolvedValue(mockLoginResponse);
    jest.spyOn(userService, 'login').mockImplementation(mockLogin);

    render(<Auth />);
    
    const emailInput = screen.getByLabelText(/email/i);
    const passwordInput = screen.getByLabelText(/пароль/i);
    const submitButton = screen.getByRole('button', { name: /войти/i });

    // Заполняем форму
    fireEvent.change(emailInput, { target: { value: 'test@example.com' } });
    fireEvent.change(passwordInput, { target: { value: 'password123' } });

    // Отправляем форму внутри act
    await act(async () => {
      fireEvent.click(submitButton);
    });

    // Проверяем, что login был вызван с правильными параметрами
    expect(mockLogin).toHaveBeenCalledWith({
      email: 'test@example.com',
      password: 'password123'
    });
  });

  test('displays error message on login failure', async () => {
    const mockError = new Error('Неверный email или пароль');
    const mockLogin = jest.fn().mockRejectedValue(mockError);
    jest.spyOn(userService, 'login').mockImplementation(mockLogin);

    render(<Auth />);
    
    const emailInput = screen.getByLabelText(/email/i);
    const passwordInput = screen.getByLabelText(/пароль/i);
    const submitButton = screen.getByRole('button', { name: /войти/i });

    // Заполняем форму
    fireEvent.change(emailInput, { target: { value: 'test@example.com' } });
    fireEvent.change(passwordInput, { target: { value: 'wrongpassword' } });

    // Отправляем форму внутри act
    await act(async () => {
      fireEvent.click(submitButton);
    });

    // Проверяем, что ошибка отображается на экране
    expect(screen.getByText(/неверный email или пароль/i)).toBeInTheDocument();
  });

  test('handles successful registration', async () => {
    const mockRegisterResponse = { message: 'Регистрация успешна' };
    const mockRegister = jest.fn().mockResolvedValue(mockRegisterResponse);
    jest.spyOn(userService, 'register').mockImplementation(mockRegister);

    render(<Auth />);
    
    // Переключаемся на форму регистрации
    const switchButton = screen.getByRole('button', { name: /зарегистрироваться/i });
    await act(async () => {
      fireEvent.click(switchButton);
    });

    // Заполняем форму регистрации
    const usernameInput = screen.getByLabelText(/имя пользователя/i);
    const emailInput = screen.getByLabelText(/email/i);
    const passwordInput = screen.getByLabelText(/пароль/i);
    const submitButton = screen.getByRole('button', { name: /зарегистрироваться/i });

    fireEvent.change(usernameInput, { target: { value: 'testuser' } });
    fireEvent.change(emailInput, { target: { value: 'test@example.com' } });
    fireEvent.change(passwordInput, { target: { value: 'password123' } });

    await act(async () => {
      fireEvent.click(submitButton);
    });

    expect(mockRegister).toHaveBeenCalledWith({
      username: 'testuser',
      email: 'test@example.com',
      password: 'password123'
    });
  });

  test('displays error message on registration failure', async () => {
    const mockError = new Error('Пользователь с таким email уже существует');
    const mockRegister = jest.fn().mockRejectedValue(mockError);
    jest.spyOn(userService, 'register').mockImplementation(mockRegister);

    render(<Auth />);
    
    // Переключаемся на форму регистрации
    const switchButton = screen.getByRole('button', { name: /зарегистрироваться/i });
    await act(async () => {
      fireEvent.click(switchButton);
    });

    // Заполняем форму регистрации
    const usernameInput = screen.getByLabelText(/имя пользователя/i);
    const emailInput = screen.getByLabelText(/email/i);
    const passwordInput = screen.getByLabelText(/пароль/i);
    const submitButton = screen.getByRole('button', { name: /зарегистрироваться/i });

    fireEvent.change(usernameInput, { target: { value: 'testuser' } });
    fireEvent.change(emailInput, { target: { value: 'existing@example.com' } });
    fireEvent.change(passwordInput, { target: { value: 'password123' } });

    await act(async () => {
      fireEvent.click(submitButton);
    });

    expect(screen.getByText(/пользователь с таким email уже существует/i)).toBeInTheDocument();
  });

  test('validates required fields', async () => {
    render(<Auth />);
    
    const submitButton = screen.getByRole('button', { name: /войти/i });

    // Пытаемся отправить пустую форму
    await act(async () => {
      fireEvent.click(submitButton);
    });

    // Проверяем, что поля помечены как обязательные
    const emailInput = screen.getByLabelText(/email/i);
    const passwordInput = screen.getByLabelText(/пароль/i);

    expect(emailInput).toBeRequired();
    expect(passwordInput).toBeRequired();
  });
}); 