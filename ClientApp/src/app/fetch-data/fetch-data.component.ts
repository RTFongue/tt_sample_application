import { Component, Inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { DatePipe } from '@angular/common';


@Component({
  selector: 'app-fetch-data',
  templateUrl: './fetch-data.component.html'
})
export class FetchDataComponent {
  public forecasts: WeatherForecast[];
  private dtPipe: DatePipe;


  constructor(http: HttpClient, @Inject('BASE_URL') baseUrl: string, datePipe: DatePipe) {
    this.dtPipe = datePipe;

    http.get<WeatherForecast[]>(baseUrl + 'weatherforecast').subscribe(result => {
      this.forecasts = result;
    }, error => console.error(error));
  }

  public formatDate(date) {
    if (this.dtPipe === null)
      date = new Date();

    return this.dtPipe.transform(date, 'EEE dd, MMM yyyy')
  }

  public formatTime(date) {
    if (this.dtPipe === null)
      date  = new Date();

    return this.dtPipe.transform(date, 'hh a')
  }

  public getIcon(summary) {
    if (summary === 'Mild') { return 'weather_mild_icon_64px.png'; }
    else if (summary === 'Sweltering') { return 'weather_bracing_bright_shine_icon_64px.png'; }
    else if (summary === 'Balmy') { return 'weather_mild_icon_64px.png'; }
    else if (summary === 'Hot') { return 'hot.png'; }
    else if (summary === 'Cool') { return 'temperature_sweltering_icon_64px.png'; }
    else if (summary === 'Bracing') { return 'hot_dark.png'; }
    else if (summary === 'Freezing') { return 'weather_mild_icon_64px.png'; }
    else if (summary === 'Scorching') { return 'thermometer.png'; }
    else if (summary === 'Warm') { return 'summer.png'; }
    else { return 'thermometer.png'; }
  }

}

interface WeatherForecast {
  date: string;
  temperatureC: number;
  temperatureF: number;
  summary: string;
}
