import React from 'react';

const mockNavigate = jest.fn();
const mockSearchParams = new URLSearchParams();
const mockSetSearchParams = jest.fn();

export const BrowserRouter = ({ children }: { children: React.ReactNode }) => <div>{children}</div>;
export const Routes = ({ children }: { children: React.ReactNode }) => <div>{children}</div>;
export const Route = ({ children }: { children: React.ReactNode }) => <div>{children}</div>;
export const Navigate = ({ to }: { to: string }) => <div>Navigate to {to}</div>;
export const useNavigate = jest.fn();
export const useSearchParams = () => [mockSearchParams, mockSetSearchParams];

export const createBrowserRouter = jest.fn(() => ({}));
export const RouterProvider = ({ router }: { router: any }) => <div>Router Mock</div>;
export const useLocation = jest.fn();
export const useParams = jest.fn();
export const Link = ({ to, children }: { to: string; children: React.ReactNode }) => (
  <a href={to}>{children}</a>
); 