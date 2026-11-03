using System.ComponentModel.DataAnnotations;
namespace CafeOrdering.ViewModels;
public class CheckoutViewModel { [Required, StringLength(100)] public string CustomerName {get;set;}=""; [Required, Phone] public string Phone {get;set;}=""; [Required, EmailAddress] public string Email {get;set;}=""; [Required, StringLength(200)] public string Location {get;set;}=""; [Required, StringLength(1000)] public string LocationDetails {get;set;}=""; public string CartJson {get;set;}="[]"; }
public class CartLine { public int ProductId {get;set;} public int Quantity {get;set;} }
