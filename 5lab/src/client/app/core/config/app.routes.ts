import { Routes } from '@angular/router';

import { adminGuard, authGuard, customerGuard, guestGuard } from '../../layout/guards';

export const routes: Routes = [
  {
    path: 'login',
    canActivate: [guestGuard],
    loadComponent: () => import('../../features/auth/login/login').then((m) => m.LoginPage),
    title: 'Вход — Муркет',
  },
  {
    path: 'recovery',
    canActivate: [guestGuard],
    loadComponent: () => import('../../features/auth/recovery-contact/recovery').then((m) => m.RecoveryContactPage),
    title: 'Восстановление доступа — Муркет',
  },
  {
    path: 'recovery/sent',
    canActivate: [guestGuard],
    loadComponent: () => import('../../features/auth/recovery-sent/recovery-sent').then((m) => m.RecoverySentPage),
    title: 'Код восстановления — Муркет',
  },
  {
    path: 'reset-password',
    canActivate: [guestGuard],
    loadComponent: () => import('../../features/auth/reset-password/reset-password').then((m) => m.ResetPasswordPage),
    title: 'Новый пароль — Муркет',
  },
  {
    path: 'forbidden',
    loadComponent: () => import('../../layout/pages/forbidden/forbidden').then((m) => m.ForbiddenPage),
    title: 'Доступ запрещён — Муркет',
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('../../layout/shell/shell').then((m) => m.Shell),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'catalog' },
      {
        path: 'catalog',
        loadComponent: () => import('../../features/catalog/catalog-page/catalog-page').then((m) => m.CatalogPage),
        title: 'Каталог — Муркет',
      },
      {
        path: 'products/:id',
        loadComponent: () => import('../../features/catalog/product-page/product-page').then((m) => m.ProductPage),
        title: 'Товар — Муркет',
      },
      {
        path: 'cart',
        loadComponent: () => import('../../features/cart/cart-page/cart-page').then((m) => m.CartPage),
        title: 'Корзина — Муркет',
      },
      {
        path: 'checkout',
        canActivate: [customerGuard],
        loadComponent: () => import('../../features/orders/checkout-page/checkout-page').then((m) => m.CheckoutPage),
        title: 'Оформление заказа — Муркет',
      },
      {
        path: 'checkout/success/:number',
        canActivate: [customerGuard],
        loadComponent: () =>
          import('../../features/orders/checkout-success/checkout-success').then((m) => m.CheckoutSuccessPage),
        title: 'Заказ оформлен — Муркет',
      },
      {
        path: 'account',
        loadComponent: () => import('../../features/profile/account-layout/account-layout').then((m) => m.AccountLayout),
        children: [
          { path: '', pathMatch: 'full', loadComponent: () => import('../../features/profile/profile-data/profile-data').then((m) => m.ProfileDataPage), title: 'Личный кабинет — Муркет' },
          { path: 'addresses', loadComponent: () => import('../../features/profile/addresses/addresses').then((m) => m.AddressesPage), title: 'Адреса доставки — Муркет' },
          { path: 'orders', loadComponent: () => import('../../features/orders/orders-list/orders-list').then((m) => m.OrdersPage), title: 'Мои заказы — Муркет' },
          { path: 'orders/:id', loadComponent: () => import('../../features/orders/order-detail/order-detail').then((m) => m.OrderDetailPage), title: 'Заказ — Муркет' },
          { path: 'security', loadComponent: () => import('../../features/profile/security/security').then((m) => m.SecurityPage), title: 'Безопасность — Муркет' },
        ],
      },
      {
        path: 'admin',
        canActivate: [adminGuard],
        loadComponent: () => import('../../features/admin/admin-layout/admin-layout').then((m) => m.AdminLayout),
        children: [
          { path: '', pathMatch: 'full', redirectTo: 'products' },
          { path: 'products', loadComponent: () => import('../../features/admin/products/products').then((m) => m.AdminProductsPage), title: 'Товары — Управление Муркет' },
          { path: 'orders', loadComponent: () => import('../../features/admin/orders/orders').then((m) => m.AdminOrdersPage), title: 'Заказы — Управление Муркет' },
          { path: 'users', loadComponent: () => import('../../features/admin/users/users').then((m) => m.AdminUsersPage), title: 'Пользователи — Управление Муркет' },
        ],
      },
    ],
  },
  {
    path: '**',
    loadComponent: () => import('../../layout/pages/not-found/not-found').then((m) => m.NotFoundPage),
    title: 'Страница не найдена — Муркет',
  },
];
