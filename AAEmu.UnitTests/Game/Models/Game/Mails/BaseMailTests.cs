using AAEmu.Game.Models.Game.Mails;

namespace AAEmu.UnitTests.Game.Models.Game.Mails;

/// <summary>
/// Regression tests for the mail return-to-sender logic (issue #974).
/// A mail that was already returned to its sender must never become eligible for
/// another return pass, otherwise it ping-pongs between sender and receiver every
/// time another character is deleted (DeleteCharacterAssets).
/// BaseMail.CanReturnMail() is pure state logic and does not touch any manager.
/// </summary>
public class BaseMailTests
{
    private static BaseMail CreateNormalMail(uint senderId, uint receiverId)
    {
        return new BaseMail
        {
            Id = 1,
            MailType = MailType.Normal,
            Title = "test",
            ReceiverName = "receiver",
            Header =
            {
                Status = MailStatus.Unread,
                SenderId = senderId,
                SenderName = "sender",
                ReceiverId = receiverId,
                Returned = false
            }
        };
    }

    [Test]
    public async Task CanReturnMail_UndeliveredNormalMail_ReturnsTrue()
    {
        // Arrange - a mail in transit to an offline player
        var mail = CreateNormalMail(senderId: 100, receiverId: 200);
        mail.IsDelivered = false;

        // Act
        var canReturn = mail.CanReturnMail();

        // Assert
        await Assert.That(canReturn).IsTrue();
    }

    [Test]
    public async Task CanReturnMail_DeliveredMail_ReturnsFalse()
    {
        // Arrange
        var mail = CreateNormalMail(senderId: 100, receiverId: 200);
        mail.IsDelivered = true;

        // Act
        var canReturn = mail.CanReturnMail();

        // Assert
        await Assert.That(canReturn).IsFalse();
    }

    [Test]
    public async Task CanReturnMail_AlreadyReturnedMail_ReturnsFalse()
    {
        // Arrange - fix #974: the mail was returned once, Header.Returned is now set
        var mail = CreateNormalMail(senderId: 200, receiverId: 100);
        mail.IsDelivered = false;
        mail.Header.Returned = true;

        // Act
        var canReturn = mail.CanReturnMail();

        // Assert - a second return pass must be blocked to prevent ping-ponging
        await Assert.That(canReturn).IsFalse();
    }

    [Test]
    public async Task CanReturnMail_SystemMailWithZeroSender_ReturnsFalse()
    {
        // Arrange - system mails (sender id 0) have nobody to return to
        var mail = CreateNormalMail(senderId: 0, receiverId: 200);
        mail.IsDelivered = false;

        // Act
        var canReturn = mail.CanReturnMail();

        // Assert
        await Assert.That(canReturn).IsFalse();
    }

    [Test]
    public async Task CanReturnMail_MailToSelf_ReturnsFalse()
    {
        // Arrange
        var mail = CreateNormalMail(senderId: 100, receiverId: 100);
        mail.IsDelivered = false;

        // Act
        var canReturn = mail.CanReturnMail();

        // Assert
        await Assert.That(canReturn).IsFalse();
    }

    [Test]
    public async Task CanReturnMail_ChargedMail_ReturnsFalse()
    {
        // Arrange - only Normal and Express mails support returning
        var mail = CreateNormalMail(senderId: 100, receiverId: 200);
        mail.MailType = MailType.Charged;
        mail.IsDelivered = false;

        // Act
        var canReturn = mail.CanReturnMail();

        // Assert
        await Assert.That(canReturn).IsFalse();
    }
}
