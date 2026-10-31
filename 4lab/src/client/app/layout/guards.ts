import { inject } from '@angular/core';
import { CanActivateFn, Router, UrlTree } from '@angular/router';
import { map } from 'rxjs';

import { AuthService } from '../core/services/auth.service';

function whenReady(decide: (auth: AuthService, router: Router) => boolean | UrlTree) {
  return () => {
    const auth = inject(AuthService);
    const router = inject(Router);
    return auth.ready$.pipe(map(() => decide(auth, router)));
  };
}

/** Неавторизованный → /login с возвратом обратно. */
export const authGuard: CanActivateFn = whenReady((auth, router) =>
  auth.user() ? true : router.createUrlTree(['/login']),
);

/** Авторизованный не видит экраны входа/восстановления. */
export const guestGuard: CanActivateFn = whenReady((auth, router) =>
  auth.user() ? router.createUrlTree(['/catalog']) : true,
);

/** Оформление заказа — только для покупателя. */
export const customerGuard: CanActivateFn = whenReady((auth, router) =>
  auth.user()?.role === 'customer' ? true : router.createUrlTree(['/forbidden']),
);

/** Раздел «Управление» — только для администратора. */
export const adminGuard: CanActivateFn = whenReady((auth, router) =>
  auth.user()?.role === 'admin' ? true : router.createUrlTree(['/forbidden']),
);
