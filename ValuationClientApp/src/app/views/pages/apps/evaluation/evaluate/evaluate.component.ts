import { NgClass, NgFor } from '@angular/common';
import { OnInit, Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PdfViewerModule } from 'ng2-pdf-viewer';
import { ToastrService } from 'ngx-toastr';
import { EvaluationService } from '../../../services/evaluation.service';
import { encode, decode } from 'js-base64';
import { ActivatedRoute } from '@angular/router'

@Component({
  selector: 'app-evaluate',
  imports: [NgClass, NgFor, CommonModule, PdfViewerModule],
  templateUrl: './evaluate.component.html',
  styleUrl: './evaluate.component.scss'
})

export class EvaluateComponent implements OnInit {
  primaryData: any;
  qaList: any[] = [];
  partList: any[] = [];
  groupList: any[] = [];
  totalMarks: number = 0;
  obtainedMarks: number = 0;
  activeQuestion: string = '---------- Please select the question above to load here ----------';
  activeQuestionImg: string = '';
  activeAnswer: string = '---------- Answerkey loads here ----------';
  activeQuestionMark: string = '';
  src = "https://vadimdez.github.io/ng2-pdf-viewer/assets/pdf-test.pdf";
  //src = 'https://iris.who.int/bitstream/handle/10665/137592/roadmapsitrep_7Nov2014_eng.pdf';

  constructor(
    private route: ActivatedRoute,
    private toastr: ToastrService,
    private evaluationService: EvaluationService
  ) { }

  ngOnInit(): void {
    console.log("ngOnInit.................");
    this.route.paramMap.subscribe(params => {
      let encodedPrimaryData = params.get('data') || "";
      this.primaryData = JSON.parse(decode(encodedPrimaryData));
      console.log(this.primaryData);
      if (this.primaryData.answersheetId) {
        this.loadQuestionPaperAnswerKey(this.primaryData.answersheetId);
      }
    });
  }

  loadQuestionPaperAnswerKey(answerSheetId: number) {
    console.log('loading questions............');
    this.evaluationService.getQuestionAndAnswer(answerSheetId).subscribe(
      (data: any[]) => {
        if (data.length > 0) {

          this.getPartList(data);
          this.getGroupList(data);

          data.forEach((item) => {
            this.qaList.push({
              "questionId": item.questionNumber,
              "questionPart": item.questionPartName,
              "questionGroup": item.questionGroupName,
              "questionNumber": item.questionNumberDisplay,
              "questionDescription": decode(item.questionDescription),
              "questionImage": decode(item.questionImage),
              "answerDescription": decode(item.answerDescription),
              "mark": this.getMark(item.questionMark)
            });
          });

          this.reduceChoiceQuestionMarks();
          console.log("qaList: ", this.qaList);
        }
      },
      (error) => {
        console.error('Error fetching question and answer:', error);
        this.toastr.error('Failed to load question and answer.');
      }
    );
  }

  getMark(questionMark: string) {
    let mark = 0;
    const dom = new DOMParser().parseFromString(decode(questionMark), 'text/html');
    let spans = dom.querySelectorAll('span');
    spans.forEach((span) => {
      let innerHTML = span.innerHTML.replace(/&nbsp;/g, '').trim();
      if ((innerHTML.length > 0)) {
        mark = parseInt(innerHTML);
      }
    });
    this.totalMarks = this.totalMarks + mark;

    return mark;
  }

  reduceChoiceQuestionMarks() {
     // Remove marks if choice question
     this.groupList.forEach((group) => {    
      if (group.count == 2) {
        let item = this.qaList.find(e => e.questionGroup === group.group);
        if (item) {
          this.totalMarks = this.totalMarks - item.mark;
          return;
        }
      }
      if (group.count == 4) {
        let item = this.qaList.find(e => e.questionGroup === group.group);
        if (item) {
          this.totalMarks = this.totalMarks - (item.mark * 2);
          return;
        }
      }
    });
  }

  getPartList(data: any[]) {
    console.log("getPartList...............");
    const partMap = new Map();
    data.forEach(({ questionPartName }) => {
      partMap.set(questionPartName, (partMap.get(questionPartName) || 0) + 1);
    });
    this.partList = Array.from(partMap, ([part, count]) => ({ part, count }));
    console.log("partList:", this.partList);
  }

  getGroupList(data: any[]) {
    console.log("getGroupList...............");
    const groupMap = new Map();
    data.forEach(({ questionGroupName }) => {
      groupMap.set(questionGroupName, (groupMap.get(questionGroupName) || 0) + 1);
    });
    this.groupList = Array.from(groupMap, ([group, count]) => ({ group, count }))
      .filter(({ count }) => count > 1);
    console.log("groupList:", this.groupList);
  }

  getQuestionDetails(event: any) {
    console.log("getting question.................");

    let btnList = document.querySelectorAll(".question .btn");
    let txtList = document.querySelectorAll(".question .form-control");
    let txtid = `#${event.target.id.replace('btn', 'txt')}`;
    let txt = document.querySelector(txtid);

    btnList.forEach((item) => {
      if (item.classList.contains('active')) {
        item.classList.remove('active');
      }
    });
    txtList.forEach((item) => {
      if (item.classList.contains('active')) {
        item.classList.remove('active');
      }
    });

    event.srcElement.classList.add('active');
    txt?.classList.add('active');

    let matchedItem = this.qaList.filter(x => x.questionNumber == event.target.innerText)[0];
    //console.log(matchedItem);
    this.activeQuestion = matchedItem.questionDescription;
    this.activeQuestionImg = matchedItem.questionImage;
    this.activeAnswer = matchedItem.answerDescription;
    this.activeQuestionMark = matchedItem.mark;
  }

  highlightQuestion(event: any) {
    // let btnList = document.querySelectorAll(".question .btn");
    // let txtList = document.querySelectorAll(".question .form-control");
    // let btnId = `#${event.target.id.replace('txt','btn')}`;
    // let btn = document.querySelector(btnId)

    // btnList.forEach((item) => {
    //   if (item.classList.contains('active')) {
    //     item.classList.remove('active');
    //   }
    // });
    // txtList.forEach((item) => {
    //   if (item.classList.contains('active')) {
    //     item.classList.remove('active');
    //   }
    // });

    // event.srcElement.classList.add('active');
    // btn?.classList.add('active');
  }

  validateMark(event: any, maxMark: number) {
    if (event.target.value) {
      if (event.target.value.match(/[^0-9]/g)) {
        this.toastr.error('Please add only numbers.');
        event.target.value = event.target.value.replace(/[^\d]/g, '');
        return;
      }
      else if ((parseFloat(event.target.value) > parseInt(event.srcElement.max))) {
        let msg = `Maximum marks allowed is ${maxMark}`
        this.toastr.error(msg);
        event.target.value = '';
        return;
      }

      //console.log(event.target.value + " > " + event.srcElement.max + " = " + (parseFloat(event.target.value) > parseInt(event.srcElement.max)));
      //console.log(event.target.value + " < " + event.srcElement.max + " = " + (parseFloat(event.target.value) < parseInt(event.srcElement.max)));
      //console.log((parseFloat(event.target.value) < parseInt(event.srcElement.max)));
      //set textbox color red if alloted mark greater than the question mark
      // if ((event.target.value < 0) || (parseFloat(event.target.value) > parseInt(event.srcElement.max))) {
      //   event.srcElement.classList.add('error');
      // }
      // else {
      //   event.srcElement.classList.remove('error');
      // }
      this.obtainedMarks = 0;
      let txtList = document.querySelectorAll(".question .form-control") as NodeListOf<HTMLInputElement>;
      txtList.forEach((item: HTMLInputElement) => {
        if (item.value) {
          this.obtainedMarks += parseFloat(item.value); // sum all text box marks
        }
      });
    }
  }

  submitEvaluation() {
    if (this.obtainedMarks == 0) {
      this.toastr.warning('Obained Marks is 0. Please evaluate.');
    }
    else if (this.obtainedMarks > this.totalMarks) {
      this.toastr.error('Obtained Marks are more than Total marks. Please verify.');
    }
    else {
      this.toastr.success("Evaluation submitted successfully");
    }
  }

  filter(part: string) {
    let list = this.qaList.filter(e => e.part === part);
    return list;
  }

  onError(error: any) {
    console.error('PDF error: ', error);
  }

}
