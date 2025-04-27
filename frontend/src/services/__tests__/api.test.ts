import axios from 'axios';
import { login, register, getProfile } from '../api';

jest.mock('axios');
const mockedAxios = axios as jest.Mocked<typeof axios>;

describe('API Service', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  describe('login', () => {
    test('successful login', async () => {
      const mockResponse = { data: { token: 'test-token', userId: '123' } };
      mockedAxios.post.mockResolvedValueOnce(mockResponse);

      const result = await login('testuser', 'password');
      
      expect(result).toEqual(mockResponse.data);
      expect(mockedAxios.post).toHaveBeenCalledWith('/api/auth/login', {
        username: 'testuser',
        password: 'password'
      });
    });

    test('failed login', async () => {
      mockedAxios.post.mockRejectedValueOnce(new Error('Invalid credentials'));

      await expect(login('testuser', 'wrong-password'))
        .rejects
        .toThrow('Invalid credentials');
    });
  });

  describe('register', () => {
    test('successful registration', async () => {
      const mockResponse = { data: { message: 'Registration successful' } };
      mockedAxios.post.mockResolvedValueOnce(mockResponse);

      const result = await register('testuser', 'test@email.com', 'password');
      
      expect(result).toEqual(mockResponse.data);
      expect(mockedAxios.post).toHaveBeenCalledWith('/api/auth/register', {
        username: 'testuser',
        email: 'test@email.com',
        password: 'password'
      });
    });
  });

  describe('getProfile', () => {
    test('fetches user profile', async () => {
      const mockProfile = { data: { id: '123', username: 'testuser', email: 'test@email.com' } };
      mockedAxios.get.mockResolvedValueOnce(mockProfile);

      const result = await getProfile('123');
      
      expect(result).toEqual(mockProfile.data);
      expect(mockedAxios.get).toHaveBeenCalledWith('/api/users/123');
    });
  });
}); 