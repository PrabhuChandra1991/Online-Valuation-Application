import { Injectable } from '@angular/core';
import { Observable, Subject } from 'rxjs';

@Injectable({
    providedIn: 'root'
})

export class SharedService {
  private actionSource = new Subject<string>();
  action$ = this.actionSource.asObservable();

  callAction(action: string) {
    this.actionSource.next(action);
  }
}