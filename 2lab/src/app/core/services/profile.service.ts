import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { Address, AddressDraft, Profile } from '../../shared/models';

import { API_BASE_URL } from '../config/api';

@Injectable({ providedIn: 'root' })
export class ProfileService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${API_BASE_URL}/me`;

  profile(): Observable<Profile> {
    return this.http.get<Profile>(`${this.apiUrl}/profile`);
  }

  updateProfile(profile: Profile): Observable<Profile> {
    return this.http.put<Profile>(`${this.apiUrl}/profile`, profile);
  }

  changePassword(currentPassword: string, password: string): Observable<unknown> {
    return this.http.put(`${this.apiUrl}/password`, { currentPassword, password });
  }

  addresses(): Observable<Address[]> {
    return this.http.get<Address[]>(`${this.apiUrl}/addresses`);
  }

  createAddress(draft: AddressDraft): Observable<Address> {
    return this.http.post<Address>(`${this.apiUrl}/addresses`, draft);
  }

  updateAddress(id: string, draft: AddressDraft): Observable<Address> {
    return this.http.put<Address>(`${this.apiUrl}/addresses/${id}`, draft);
  }

  deleteAddress(id: string): Observable<unknown> {
    return this.http.delete(`${this.apiUrl}/addresses/${id}`);
  }
}
