import { EventEmitter, Injectable, Output } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class SpinnerService {

  constructor() { }

  @Output() change: EventEmitter<any> = new EventEmitter();

  toggleSpinnerState(data: boolean): any {
    this.change.emit(data);
  }
}
