import { createBrowserRouter, RouterProvider, Navigate } from 'react-router-dom'
import Shell from '../src/ui/layout/Shell'
import Login from '../src/ui/pages/Login'
import Register from '../src/ui/pages/Register'
import Rooms from '../src/ui/pages/Rooms'
import RoomChat from '../src/ui/pages/RoomChat'

const router = createBrowserRouter([
  { path: '/login', element: <Login /> },
  { path: '/register', element: <Register /> },
  {
    path: '/',
    element: <Shell />,
    children: [
      { index: true, element: <Navigate to="/rooms" replace /> },
      { path: 'rooms', element: <Rooms /> },
      { path: 'rooms/:roomId', element: <RoomChat /> },
    ],
  },
  { path: '*', element: <Navigate to="/" replace /> },
])

export default function App() {
  return <RouterProvider router={router} />
}