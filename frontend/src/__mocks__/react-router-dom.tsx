import React from 'react';

const mockNavigate = jest.fn();
const mockSearchParams = new URLSearchParams();
const mockSetSearchParams = jest.fn();

export const BrowserRouter = ({ children }: { children: React.ReactNode }) => <div>{children}</div>;
export const Routes = ({ children }: { children: React.ReactNode }) => <div>{children}</div>;
export const Route = ({ children }: { children: React.ReactNode }) => <div>{children}</div>;
export const Navigate = () => <div>Navigate</div>;
export const useNavigate = () => mockNavigate;
export const useSearchParams = () => [mockSearchParams, mockSetSearchParams]; 