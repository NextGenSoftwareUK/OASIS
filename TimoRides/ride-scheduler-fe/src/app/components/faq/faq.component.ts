import { Component } from '@angular/core';

@Component({
  selector: 'app-faq',
  templateUrl: './faq.component.html',
  styleUrls: ['./faq.component.css'],
})
export class FaqComponent {
  faqs = [
    {
      question: 'About us',
      answer:
        'We are a mobility platform that connects drivers and people looking for taxi services.\n We allow our clients not only to pre-book the ride online but to select and review the ride of their choice taking into consideration the car state, drivers statistics and experience.',
    },
    {
      question: 'Our Vision',
      answer:
        'To be the most desirable safe and green on schedule mobility platform that grant people safe & fast access to a ride anytime, anywhere.',
    },
    {
      question: 'How does it work?',
      answer: `
      1.       Enter your pickup point, time, date and destination \n

      2.       Pick and review a driver from the options available \n

      3.       Proceed to payment \n

      4.       Receive a booking invoice confirmation \n

      5.       Timo driver picks you up at a given location and time \n

      6.       Provide the driver with the start code at the beginning of the trip \n

      7.       Provide the driver with the end code at the end of the trip \n
      `,
    },
    {
      question: 'How can I pay?',
      answer:
        'Timo accepts all major payment methods including MasterCard, Visa and American Express.',
    },
    {
      question: 'Timo Benefits',
      answer:
        'The industry faces severe challenges, including cars in a dilapidated state, bad drivers, frequent cancelled trips, driver strikes, and passengers being attacked and robbed. Our platform empowers our customers by allowing them to review the car state, driver stats, experience and make an informed decision based on the car they want. Our solution also reduces the chances of frequent trip cancellations as it allows both the customers and the driver ample time to do so before inconveniencing one another. Our cashless approach and the review process depict that the occurrences of any eventuality such as crime is unlikely to happen.',
    },
    {
      question: 'How can I request for refund',
      answer:
        'To initiate a refund request, kindly send an email to info@timorides.com and provide us with the evidence of booking the ride.',
    },
  ];
}
