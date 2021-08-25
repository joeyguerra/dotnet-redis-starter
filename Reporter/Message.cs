using System;
using System.Collections;

namespace Reporter {
    public class Message : IEquatable<Message> {
        public string Label {get;set;}
        public string SKU {get;set;}
        public int QTY {get;set;}
        public string PONumber {get;set;}
        public int TotalAmount {get;set;}
        public string Submitter {get;set;}
        public string Status {get;set;}

        public bool Equals(Message other)
        {
            return this.Label == other.Label
                && this.SKU == other.SKU
                && this.QTY == other.QTY
                && this.PONumber == other.PONumber
                && this.TotalAmount == other.TotalAmount
                && this.Submitter == other.Submitter
                && this.Status == other.Status;
        }
    }
}