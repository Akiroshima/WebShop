import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';

import { AdminService, AdminUser } from '../../../core/services/admin.service';
import { NotificationService } from '../../../core/services/notification.service';
import { RuDatePipe } from '../../../shared/pipes/ru-date.pipe';

@Component({
  selector: 'app-admin-users-page',
  imports: [RuDatePipe],
  templateUrl: './users.html',
  styleUrl: './users.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminUsersPage {
  private readonly adminApi = inject(AdminService);
  private readonly notifications = inject(NotificationService);

  readonly users = signal<AdminUser[]>([]);
  readonly loading = signal(true);

  constructor() {
    this.adminApi.users().subscribe({
      next: (users) => {
        this.users.set(users);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.notifications.error(NotificationService.messageOf(err));
      },
    });
  }
}
