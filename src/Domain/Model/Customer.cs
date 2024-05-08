// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Domain.Model;

using BridgingIT.DevKit.Domain.Model;
using EnsureThat;

public class Customer : AggregateRoot<CustomerId, Guid>
{
    private Customer()
    {
    }

    private Customer(string firstName, string lastName, EmailAddress email)
    {
        this.FirstName = firstName;
        this.LastName = lastName;
        this.Email = email;
    }

    public string FirstName { get; private set; }

    public string LastName { get; private set; }

    public EmailAddress Email { get; private set; }

    public static Customer Create(string firstName, string lastName, string email)
    {
        var customer = new Customer(firstName, lastName, EmailAddress.Create(email));

        customer.DomainEvents.Register(
            new CustomerCreatedDomainEvent(customer));

        return customer;
    }

    public Customer ChangeName(string firstName, string lastName)
    {
        if (string.IsNullOrEmpty(firstName) && string.IsNullOrEmpty(lastName))
        {
            return this;
        }

        this.FirstName = firstName;
        this.LastName = lastName;

        this.DomainEvents.Register(
            new CustomerUpdatedDomainEvent(this)/*, true*/);

        return this;
    }
}
