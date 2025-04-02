import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

@Injectable({
    providedIn: 'root'
})

export class InstituteService {

    private apiUrl = environment.apiURL; // Update this with your actual API URL

    private httpOptions = {
        headers: new HttpHeaders({
          'Content-Type': 'application/json'
        })
      };

      constructor(private http: HttpClient) { }

    getInstitutions(): Observable<any> {
        return this.http.get(`${this.apiUrl}/api/Institution`);
    }
  }
