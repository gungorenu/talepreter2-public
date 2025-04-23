FROM rabbitmq:3.13.2-management-alpine

# commented out until we fix the virtual host >> user/pwd issue
#RUN touch /etc/rabbitmq/definitions.json
#RUN touch /etc/rabbitmq/rabbitmq.conf

COPY ./RabbitMQ/rabbitmq_delayed_message_exchange-3.13.0.ez /opt/rabbitmq/plugins/
RUN rabbitmq-plugins enable rabbitmq_delayed_message_exchange
