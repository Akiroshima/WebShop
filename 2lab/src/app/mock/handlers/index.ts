import { MockHttpError } from '../mock-error';

import {
  handleLogin,
  handleLogout,
  handleMe,
  handleRecoveryConfirm,
  handleRecoveryRequest,
  handleResetPassword,
} from './auth.handlers';
import { handleBrands, handleCategories, handleProductDetail, handleProductList } from './catalog.handlers';
import {
  handleAddItem,
  handleApplyPromo,
  handleGetCart,
  handleRemoveItem,
  handleRemovePromo,
  handleUpdateItemQty,
} from './cart.handlers';
import { handleAdminCreateProduct, handleAdminDeleteProduct, handleAdminOrderStatus, handleAdminOrders, handleAdminProducts, handleAdminUpdateProduct, handleAdminUsers } from './admin.handlers';
import { handleCreateOrder, handleMyOrderDetail, handleMyOrders } from './orders.handlers';
import {
  handleChangePassword,
  handleCreateAddress,
  handleDeleteAddress,
  handleGetProfile,
  handleListAddresses,
  handleUpdateAddress,
  handleUpdateProfile,
} from './profile.handlers';
import { MockRequestContext, MockResponse } from './context';

type RouteHandler = (match: RegExpMatchArray, ctx: MockRequestContext) => MockResponse;

interface Route {
  method: string;
  pattern: RegExp;
  handler: RouteHandler;
}

const routes: Route[] = [
  // Аутентификация и восстановление доступа
  { method: 'POST', pattern: /^\/api\/v1\/auth\/login$/, handler: (_m, ctx) => handleLogin(ctx) },
  { method: 'POST', pattern: /^\/api\/v1\/auth\/logout$/, handler: () => handleLogout() },
  { method: 'GET', pattern: /^\/api\/v1\/auth\/me$/, handler: (_m, ctx) => handleMe(ctx) },
  { method: 'POST', pattern: /^\/api\/v1\/auth\/recovery\/request$/, handler: (_m, ctx) => handleRecoveryRequest(ctx) },
  { method: 'POST', pattern: /^\/api\/v1\/auth\/recovery\/confirm$/, handler: (_m, ctx) => handleRecoveryConfirm(ctx) },
  { method: 'POST', pattern: /^\/api\/v1\/auth\/reset-password$/, handler: (_m, ctx) => handleResetPassword(ctx) },

  // Каталог
  { method: 'GET', pattern: /^\/api\/v1\/catalog\/categories$/, handler: () => handleCategories() },
  { method: 'GET', pattern: /^\/api\/v1\/catalog\/brands$/, handler: (_m, ctx) => handleBrands(ctx) },
  { method: 'GET', pattern: /^\/api\/v1\/catalog\/products$/, handler: (_m, ctx) => handleProductList(ctx) },
  { method: 'GET', pattern: /^\/api\/v1\/catalog\/products\/([^\/]+)$/, handler: (m, ctx) => handleProductDetail(m, ctx) },

  // Корзина
  { method: 'GET', pattern: /^\/api\/v1\/cart$/, handler: (_m, ctx) => handleGetCart(ctx) },
  { method: 'POST', pattern: /^\/api\/v1\/cart\/items$/, handler: (_m, ctx) => handleAddItem(ctx) },
  { method: 'PUT', pattern: /^\/api\/v1\/cart\/items\/([^\/]+)$/, handler: (m, ctx) => handleUpdateItemQty(m, ctx) },
  { method: 'DELETE', pattern: /^\/api\/v1\/cart\/items\/([^\/]+)$/, handler: (m, ctx) => handleRemoveItem(m, ctx) },
  { method: 'POST', pattern: /^\/api\/v1\/cart\/promo$/, handler: (_m, ctx) => handleApplyPromo(ctx) },
  { method: 'DELETE', pattern: /^\/api\/v1\/cart\/promo$/, handler: (_m, ctx) => handleRemovePromo(ctx) },

  // Заказы
  { method: 'POST', pattern: /^\/api\/v1\/orders$/, handler: (_m, ctx) => handleCreateOrder(ctx) },
  { method: 'GET', pattern: /^\/api\/v1\/orders$/, handler: (_m, ctx) => handleMyOrders(ctx) },
  { method: 'GET', pattern: /^\/api\/v1\/orders\/([^\/]+)$/, handler: (m, ctx) => handleMyOrderDetail(m, ctx) },

  // Профиль
  { method: 'GET', pattern: /^\/api\/v1\/me\/profile$/, handler: (_m, ctx) => handleGetProfile(ctx) },
  { method: 'PUT', pattern: /^\/api\/v1\/me\/profile$/, handler: (_m, ctx) => handleUpdateProfile(ctx) },
  { method: 'PUT', pattern: /^\/api\/v1\/me\/password$/, handler: (_m, ctx) => handleChangePassword(ctx) },
  { method: 'GET', pattern: /^\/api\/v1\/me\/addresses$/, handler: (_m, ctx) => handleListAddresses(ctx) },
  { method: 'POST', pattern: /^\/api\/v1\/me\/addresses$/, handler: (_m, ctx) => handleCreateAddress(ctx) },
  { method: 'PUT', pattern: /^\/api\/v1\/me\/addresses\/([^\/]+)$/, handler: (m, ctx) => handleUpdateAddress(m, ctx) },
  { method: 'DELETE', pattern: /^\/api\/v1\/me\/addresses\/([^\/]+)$/, handler: (m, ctx) => handleDeleteAddress(m, ctx) },

  // Модуль менеджера
  { method: 'GET', pattern: /^\/api\/v1\/admin\/products$/, handler: (_m, ctx) => handleAdminProducts(ctx) },
  { method: 'POST', pattern: /^\/api\/v1\/admin\/products$/, handler: (_m, ctx) => handleAdminCreateProduct(ctx) },
  { method: 'PUT', pattern: /^\/api\/v1\/admin\/products\/([^\/]+)$/, handler: (m, ctx) => handleAdminUpdateProduct(m, ctx) },
  { method: 'DELETE', pattern: /^\/api\/v1\/admin\/products\/([^\/]+)$/, handler: (m, ctx) => handleAdminDeleteProduct(m, ctx) },
  { method: 'GET', pattern: /^\/api\/v1\/admin\/orders$/, handler: (_m, ctx) => handleAdminOrders(ctx) },
  { method: 'PUT', pattern: /^\/api\/v1\/admin\/orders\/([^\/]+)\/status$/, handler: (m, ctx) => handleAdminOrderStatus(m, ctx) },
  { method: 'GET', pattern: /^\/api\/v1\/admin\/users$/, handler: (_m, ctx) => handleAdminUsers(ctx) },
];

export function dispatch(ctx: MockRequestContext): MockResponse {
  for (const route of routes) {
    if (route.method !== ctx.method) {
      continue;
    }
    const match = ctx.path.match(route.pattern);
    if (match) {
      return route.handler(match, ctx);
    }
  }
  throw MockHttpError.notFound(`Запрос не найден: ${ctx.method} ${ctx.path}`);
}
